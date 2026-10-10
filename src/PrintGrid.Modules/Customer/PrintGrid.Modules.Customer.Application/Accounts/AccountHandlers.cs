using MediatR;
using PrintGrid.Modules.Customer.Domain.Entities;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Accounts;

internal static class AccountMappings
{
    public static CustomerProfileDto Profile(global::PrintGrid.Modules.Customer.Domain.Entities.Customer x) => new(x.Id, x.Email, x.FullName, x.PhoneNumber, x.IsEmailVerified);
    public static CustomerAddressDto Address(CustomerAddress x) => new(x.Id, x.Label, x.RecipientName, x.PhoneNumber,
        x.Street, x.Ward, x.District, x.City, x.PostalCode, x.Country, x.IsDefault);
}

public class GetProfileQueryHandler(ICustomerRepository customers) : IRequestHandler<GetProfileQuery, Result<CustomerProfileDto>>
{
    public async Task<Result<CustomerProfileDto>> Handle(GetProfileQuery request, CancellationToken ct)
    {
        var customer = await customers.GetByIdAsync(request.CustomerId, ct);
        return customer is null ? Result.Failure<CustomerProfileDto>(Error.NotFound("Customer", request.CustomerId)) : AccountMappings.Profile(customer);
    }
}

public class UpdateProfileCommandHandler(ICustomerRepository customers, IUnitOfWork unitOfWork) : IRequestHandler<UpdateProfileCommand, Result<CustomerProfileDto>>
{
    public async Task<Result<CustomerProfileDto>> Handle(UpdateProfileCommand request, CancellationToken ct)
    {
        var customer = await customers.GetByIdAsync(request.CustomerId, ct);
        if (customer is null) return Result.Failure<CustomerProfileDto>(Error.NotFound("Customer", request.CustomerId));
        customer.UpdateProfile(request.FullName, request.PhoneNumber);
        customers.Update(customer);
        await unitOfWork.SaveChangesAsync(ct);
        return AccountMappings.Profile(customer);
    }
}

public class GetAddressesQueryHandler(ICustomerAddressRepository addresses) : IRequestHandler<GetAddressesQuery, Result<IReadOnlyList<CustomerAddressDto>>>
{
    public async Task<Result<IReadOnlyList<CustomerAddressDto>>> Handle(GetAddressesQuery request, CancellationToken ct) =>
        Result.Success<IReadOnlyList<CustomerAddressDto>>((await addresses.ListAsync(request.CustomerId, ct)).Select(AccountMappings.Address).ToList());
}

public class CreateAddressCommandHandler(ICustomerAddressRepository addresses, IUnitOfWork unitOfWork) : IRequestHandler<CreateAddressCommand, Result<CustomerAddressDto>>
{
    public async Task<Result<CustomerAddressDto>> Handle(CreateAddressCommand request, CancellationToken ct)
    {
        var input = request.Input;
        var existing = await addresses.ListAsync(request.CustomerId, ct);
        var makeDefault = input.IsDefault || existing.Count == 0;
        if (makeDefault) foreach (var item in existing) item.SetDefault(false);
        var address = CustomerAddress.Create(request.CustomerId, input.Label, input.RecipientName, input.PhoneNumber,
            input.Street, input.Ward, input.District, input.City, input.PostalCode, input.Country, makeDefault);
        await addresses.AddAsync(address, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return AccountMappings.Address(address);
    }
}

public class UpdateAddressCommandHandler(ICustomerAddressRepository addresses, IUnitOfWork unitOfWork) : IRequestHandler<UpdateAddressCommand, Result<CustomerAddressDto>>
{
    public async Task<Result<CustomerAddressDto>> Handle(UpdateAddressCommand request, CancellationToken ct)
    {
        var address = await addresses.GetAsync(request.CustomerId, request.AddressId, ct);
        if (address is null) return Result.Failure<CustomerAddressDto>(Error.NotFound("Address", request.AddressId));
        var input = request.Input;
        address.Update(input.Label, input.RecipientName, input.PhoneNumber, input.Street, input.Ward, input.District, input.City, input.PostalCode, input.Country);
        if (input.IsDefault)
        {
            foreach (var item in await addresses.ListAsync(request.CustomerId, ct)) item.SetDefault(item.Id == address.Id);
        }
        await unitOfWork.SaveChangesAsync(ct);
        return AccountMappings.Address(address);
    }
}

public class DeleteAddressCommandHandler(ICustomerAddressRepository addresses, IUnitOfWork unitOfWork) : IRequestHandler<DeleteAddressCommand, Result>
{
    public async Task<Result> Handle(DeleteAddressCommand request, CancellationToken ct)
    {
        var address = await addresses.GetAsync(request.CustomerId, request.AddressId, ct);
        if (address is null) return Result.Failure(Error.NotFound("Address", request.AddressId));
        if (await addresses.IsUsedByOpenOrderAsync(address, ct))
            return Result.Failure(Error.Conflict("Không thể xóa địa chỉ đang được dùng bởi đơn hàng chưa hoàn tất."));
        var remaining = (await addresses.ListAsync(request.CustomerId, ct)).Where(x => x.Id != address.Id).ToList();
        if (address.IsDefault && remaining.Count > 0) remaining[0].SetDefault(true);
        addresses.Remove(address);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
