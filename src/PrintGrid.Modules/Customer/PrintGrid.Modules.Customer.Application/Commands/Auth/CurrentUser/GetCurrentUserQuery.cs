using MediatR;
using PrintGrid.Modules.Customer.Application.DTOs;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Commands.Auth.CurrentUser;

/// <summary>
/// Reads the caller's account from the database instead of trusting the roles embedded in the
/// access token. A token minted before an account was granted an operational role still says
/// "Customer", which locks staff out of their own console (403) until the token expires.
/// Any authenticated user may call it — it only echoes their own row.
/// </summary>
public sealed record GetCurrentUserQuery(Guid UserId) : IRequest<Result<AuthUserDto>>;

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, Result<AuthUserDto>>
{
    private readonly ICustomerRepository _customers;

    public GetCurrentUserQueryHandler(ICustomerRepository customers) => _customers = customers;

    public async Task<Result<AuthUserDto>> Handle(
        GetCurrentUserQuery request,
        CancellationToken cancellationToken)
    {
        var customer = await _customers.GetByIdAsync(request.UserId, cancellationToken);
        if (customer is null || !customer.IsActive)
            return Result.Failure<AuthUserDto>(Error.Unauthorized("Account no longer exists"));

        return Result.Success(new AuthUserDto(
            customer.Id,
            customer.Email,
            customer.FullName,
            customer.Roles,
            customer.IsEmailVerified));
    }
}
