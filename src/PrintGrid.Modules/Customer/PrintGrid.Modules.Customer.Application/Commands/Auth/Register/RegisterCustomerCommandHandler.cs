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
    private readonly IEmailService _emailService;
    private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;

    public RegisterCustomerCommandHandler(
        ICustomerRepository customers,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IEmailService emailService,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        _customers = customers;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _emailService = emailService;
        _configuration = configuration;
    }

    public async Task<Result<AuthSessionDto>> Handle(
        RegisterCustomerCommand command,
        CancellationToken cancellationToken)
    {
        // Distinct code so the UI can highlight the offending field instead of
        // showing one generic "conflict" banner. (Phone numbers are intentionally
        // NOT deduplicated — one person may legitimately register several accounts
        // from the same number; email is the unique identity here.)
        if (await _customers.EmailExistsAsync(command.Email, cancellationToken))
            return Result.Failure<AuthSessionDto>(
                new Error("email_exists", $"Email '{command.Email}' is already registered"));

        var passwordHash = _passwordHasher.Hash(command.Password);

        var customer = CustomerEntity.Register(
            command.Email,
            passwordHash,
            command.FullName,
            command.PhoneNumber);

        var rawToken = Guid.NewGuid().ToString("N");
        var tokenHash = _passwordHasher.Hash(rawToken);
        customer.SetVerificationToken(tokenHash, TimeSpan.FromHours(24));

        await _customers.AddAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var baseUrl = _configuration["App:FrontendUrl"] ?? "http://localhost:5173";
        var verifyLink = $"{baseUrl}/verify-email?token={rawToken}&email={Uri.EscapeDataString(customer.Email)}";
        var emailBody = $"<p>Hi {customer.FullName},</p><p>Please verify your email by clicking <a href=\"{verifyLink}\">here</a>.</p>";
        await _emailService.SendEmailAsync(customer.Email, "Verify your PrintGrid account", emailBody, cancellationToken);

        var user = ToUserDto(customer);
        var tokens = _tokenService.CreateTokenPair(user);
        return Result.Success(new AuthSessionDto(tokens.AccessToken, tokens.RefreshToken, user));
    }

    internal static AuthUserDto ToUserDto(CustomerEntity customer) => new(
        customer.Id,
        customer.Email,
        customer.FullName,
        customer.Roles,
        customer.IsEmailVerified);
}