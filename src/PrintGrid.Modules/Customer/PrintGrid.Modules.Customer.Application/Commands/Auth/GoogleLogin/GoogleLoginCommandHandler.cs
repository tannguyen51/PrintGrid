using System.Security.Cryptography;
using MediatR;
using PrintGrid.Modules.Customer.Application.Auth;
using PrintGrid.Modules.Customer.Application.DTOs;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;
using CustomerEntity = PrintGrid.Modules.Customer.Domain.Entities.Customer;

namespace PrintGrid.Modules.Customer.Application.Commands.Auth.GoogleLogin;

public sealed class GoogleLoginCommandHandler(
    IGoogleIdentityVerifier google,
    ICustomerRepository customers,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    ITokenService tokenService)
    : IRequestHandler<GoogleLoginCommand, Result<AuthSessionDto>>
{
    public async Task<Result<AuthSessionDto>> Handle(GoogleLoginCommand command, CancellationToken cancellationToken)
    {
        var identity = await google.VerifyAsync(command.Credential, cancellationToken);
        if (identity is null || !identity.EmailVerified)
            return Result.Failure<AuthSessionDto>(Error.Unauthorized("Google credential is invalid or expired"));

        var customer = await customers.GetByGoogleSubjectAsync(identity.Subject, cancellationToken);
        if (customer is null)
        {
            customer = await customers.GetByEmailAsync(identity.Email, cancellationToken);
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
                    passwordHasher.Hash(randomPassword),
                    identity.FullName,
                    identity.Subject);
                await customers.AddAsync(customer, cancellationToken);
            }
        }

        if (!customer.IsActive)
            return Result.Failure<AuthSessionDto>(Error.Unauthorized("Account is disabled"));

        customer.RecordLogin();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var user = Login.LoginCustomerCommandHandler.ToUserDto(customer);
        var tokens = tokenService.CreateTokenPair(user, customer.SessionVersion);
        return Result.Success(new AuthSessionDto(tokens.AccessToken, tokens.RefreshToken, user));
    }
}
