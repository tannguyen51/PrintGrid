using MediatR;
using PrintGrid.Modules.Customer.Application.Auth;

using PrintGrid.Modules.Customer.Application.DTOs;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Commands.Auth.Login;

public class LoginCustomerCommandHandler : IRequestHandler<LoginCustomerCommand, Result<AuthSessionDto>>
{
    private readonly ICustomerRepository _customers;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public LoginCustomerCommandHandler(
        ICustomerRepository customers,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ITokenService tokenService)
    {
        _customers = customers;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<Result<AuthSessionDto>> Handle(
        LoginCustomerCommand command,
        CancellationToken cancellationToken)
    {
        // Defence in depth for non-SPA clients: ignore accidental surrounding
        // whitespace on both credentials. (The repository also trims the email.)
        var email = command.Email.Trim();
        var password = command.Password.Trim();

        var customer = await _customers.GetByEmailAsync(email, cancellationToken);

        // Same message for "no such account" and "wrong password" to avoid
        // leaking which emails exist.
        if (customer is null || !customer.IsActive || !_passwordHasher.Verify(password, customer.PasswordHash))
            return Result.Failure<AuthSessionDto>(Error.Unauthorized("Invalid email or password"));

        customer.RecordLogin();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var user = ToUserDto(customer);
        var tokens = _tokenService.CreateTokenPair(user);
        return Result.Success(new AuthSessionDto(tokens.AccessToken, tokens.RefreshToken, user));
    }

    internal static AuthUserDto ToUserDto(Domain.Entities.Customer customer) => new(
        customer.Id,
        customer.Email,
        customer.FullName,
        customer.Roles,
        customer.IsEmailVerified);
}
