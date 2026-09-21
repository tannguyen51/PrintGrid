using MediatR;
using PrintGrid.Modules.Customer.Application.Auth;
using PrintGrid.Modules.Customer.Application.DTOs;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Commands.Auth.Refresh;

public class RefreshSessionCommandHandler : IRequestHandler<RefreshSessionCommand, Result<AuthSessionDto>>
{
    private readonly ICustomerRepository _customers;
    private readonly ITokenService _tokenService;

    public RefreshSessionCommandHandler(
        ICustomerRepository customers,
        ITokenService tokenService)
    {
        _customers = customers;
        _tokenService = tokenService;
    }

    public async Task<Result<AuthSessionDto>> Handle(
        RefreshSessionCommand command,
        CancellationToken cancellationToken)
    {
        var customerId = _tokenService.ValidateRefreshToken(command.RefreshToken);
        if (customerId is null)
            return Result.Failure<AuthSessionDto>(Error.Unauthorized("Invalid or expired refresh token"));

        var customer = await _customers.GetByIdAsync(customerId.Value, cancellationToken);
        if (customer is null)
            return Result.Failure<AuthSessionDto>(Error.Unauthorized("Account no longer exists"));

        var user = new AuthUserDto(customer.Id, customer.Email, customer.FullName, DemoRoles.RolesFor(customer.Email));
        var tokens = _tokenService.CreateTokenPair(user);
        return Result.Success(new AuthSessionDto(tokens.AccessToken, tokens.RefreshToken, user));
    }
}