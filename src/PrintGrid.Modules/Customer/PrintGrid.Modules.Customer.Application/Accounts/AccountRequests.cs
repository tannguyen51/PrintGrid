using MediatR;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Accounts;

public record GetProfileQuery(Guid CustomerId) : IRequest<Result<CustomerProfileDto>>;
public record UpdateProfileCommand(Guid CustomerId, string FullName, string? PhoneNumber) : IRequest<Result<CustomerProfileDto>>;
public record GetAddressesQuery(Guid CustomerId) : IRequest<Result<IReadOnlyList<CustomerAddressDto>>>;
public record CreateAddressCommand(Guid CustomerId, AddressInput Input) : IRequest<Result<CustomerAddressDto>>;
public record UpdateAddressCommand(Guid CustomerId, Guid AddressId, AddressInput Input) : IRequest<Result<CustomerAddressDto>>;
public record DeleteAddressCommand(Guid CustomerId, Guid AddressId) : IRequest<Result>;
