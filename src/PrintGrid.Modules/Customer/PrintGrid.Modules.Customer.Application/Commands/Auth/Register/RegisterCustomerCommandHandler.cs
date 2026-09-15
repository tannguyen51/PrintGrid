using MediatR;
using PrintGrid.Modules.Customer.Application.Auth;
using PrintGrid.Modules.Customer.Application.DTOs;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;
using CustomerEntity = PrintGrid.Modules.Customer.Domain.Entities.Customer;

namespace PrintGrid.Modules.Customer.Application.Commands.Auth.Register;

public class RegisterCustomerCommandHandler : IRequestHandler<RegisterCustomerCommand, Result<AuthSessionDto>>
{
    private readonly ICustomerRepository _customers;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public RegisterCustomerCommandHandler(
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
        RegisterCustomerCommand command,
        CancellationToken cancellationToken)
    {
        if (await _customers.EmailExistsAsync(command.Email, cancellationToken))
            return Result.Failure<AuthSessionDto>(
                Error.Conflict($"Email '{command.Email}' is already registered"));

        var passwordHash = _passwordHasher.Hash(command.Password);

        var customer = CustomerEntity.Register(
            command.Email,
            passwordHash,
            command.FullName,
            command.PhoneNumber);

        await _customers.AddAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var user = ToUserDto(customer);
        var tokens = _tokenService.CreateTokenPair(user);
        return Result.Success(new AuthSessionDto(tokens.AccessToken, tokens.RefreshToken, user));
    }

    internal static AuthUserDto ToUserDto(CustomerEntity customer) => new(
        customer.Id,
        customer.Email,
        customer.FullName,
        new[] { "Customer" });
}