using MediatR;
using PrintGrid.Modules.Customer.Application.Auth;

using PrintGrid.Modules.Customer.Application.DTOs;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Commands.Auth.Refresh;

public class RefreshSessionCommandHandler(
    ICustomerRepository customers,
    ITokenService tokenService)
    : IRequestHandler<RefreshSessionCommand, Result<AuthSessionDto>>
{
    public async Task<Result<AuthSessionDto>> Handle(
        RefreshSessionCommand command,
        CancellationToken cancellationToken)
    {
        var identity = tokenService.ValidateRefreshToken(command.RefreshToken);
        if (identity is null)
            return Result.Failure<AuthSessionDto>(Error.Unauthorized("Invalid or expired refresh token"));

        var customer = await customers.GetByIdAsync(identity.UserId, cancellationToken);
        if (customer is null || !customer.IsActive || customer.SessionVersion != identity.SessionVersion)
            return Result.Failure<AuthSessionDto>(Error.Unauthorized("Account no longer exists"));

        var user = new AuthUserDto(customer.Id, customer.Email, customer.FullName, customer.Roles, customer.IsEmailVerified);
        var tokens = tokenService.CreateTokenPair(user, customer.SessionVersion);
        return Result.Success(new AuthSessionDto(tokens.AccessToken, tokens.RefreshToken, user));
    }
}
