using MediatR;
using PrintGrid.Modules.Customer.Application.DTOs;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Queries.Users;

public class GetUsersQueryHandler(ICustomerRepository customers)
    : IRequestHandler<GetUsersQuery, Result<IReadOnlyList<AdminUserDto>>>
{
    public async Task<Result<IReadOnlyList<AdminUserDto>>> Handle(
        GetUsersQuery request,
        CancellationToken cancellationToken)
    {
        var users = await customers.GetAllAsync(cancellationToken);

        IReadOnlyList<AdminUserDto> result =
        [
            .. users
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
        ];

        return Result.Success(result);
    }
}
