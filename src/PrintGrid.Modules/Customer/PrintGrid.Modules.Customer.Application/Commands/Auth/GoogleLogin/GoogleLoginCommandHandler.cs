using System.Security.Cryptography;
using MediatR;
using PrintGrid.Modules.Customer.Application.Auth;
using PrintGrid.Modules.Customer.Application.DTOs;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;
using CustomerEntity = PrintGrid.Modules.Customer.Domain.Entities.Customer;

namespace PrintGrid.Modules.Customer.Application.Commands.Auth.GoogleLogin;

public sealed class GoogleLoginCommandHandler : IRequestHandler<GoogleLoginCommand, Result<AuthSessionDto>>
{
    private readonly IGoogleIdentityVerifier _google;
    private readonly ICustomerRepository _customers;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public GoogleLoginCommandHandler(
        IGoogleIdentityVerifier google,
        ICustomerRepository customers,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ITokenService tokenService)
    {
        _google = google;
        _customers = customers;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<Result<AuthSessionDto>> Handle(GoogleLoginCommand command, CancellationToken cancellationToken)
    {
        var identity = await _google.VerifyAsync(command.Credential, cancellationToken);
        if (identity is null || !identity.EmailVerified)
            return Result.Failure<AuthSessionDto>(Error.Unauthorized("Google credential is invalid or expired"));

        var customer = await _customers.GetByGoogleSubjectAsync(identity.Subject, cancellationToken);
        if (customer is null)
        {
            customer = await _customers.GetByEmailAsync(identity.Email, cancellationToken);
            if (customer is not null)
            {
                if (customer.GoogleSubject is not null && customer.GoogleSubject != identity.Subject)
                    return Result.Failure<AuthSessionDto>(Error.Unauthorized("This email is linked to another Google account"));

                customer.LinkGoogleIdentity(identity.Subject);
            }
            else
            {
                var randomPassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
                customer = CustomerEntity.RegisterWithGoogle(
                    identity.Email,
                    _passwordHasher.Hash(randomPassword),
                    identity.FullName,
                    identity.Subject);
                await _customers.AddAsync(customer, cancellationToken);
            }
        }

        if (!customer.IsActive)
            return Result.Failure<AuthSessionDto>(Error.Unauthorized("Account is disabled"));

        customer.RecordLogin();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var user = Login.LoginCustomerCommandHandler.ToUserDto(customer);
        var tokens = _tokenService.CreateTokenPair(user, customer.SessionVersion);
        return Result.Success(new AuthSessionDto(tokens.AccessToken, tokens.RefreshToken, user));
    }
}
