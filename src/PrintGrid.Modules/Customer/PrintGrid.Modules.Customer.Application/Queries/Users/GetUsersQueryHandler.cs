using MediatR;
using PrintGrid.Modules.Customer.Application.DTOs;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Queries.Users;

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, Result<IReadOnlyList<AdminUserDto>>>
{
    private readonly ICustomerRepository _customers;

    public GetUsersQueryHandler(ICustomerRepository customers) => _customers = customers;

    public async Task<Result<IReadOnlyList<AdminUserDto>>> Handle(
        GetUsersQuery request,
        CancellationToken cancellationToken)
    {
        var users = await _customers.GetAllAsync(cancellationToken);

        IReadOnlyList<AdminUserDto> result = users
            .Select(user => new AdminUserDto(
                user.Id,
                user.Email,
                user.FullName,
                user.PhoneNumber,
                user.Roles,
                user.IsActive,
                user.IsEmailVerified,
                user.CreatedAt,
                user.LastLoginAt))
            .ToList();

        return Result.Success(result);
    }
}