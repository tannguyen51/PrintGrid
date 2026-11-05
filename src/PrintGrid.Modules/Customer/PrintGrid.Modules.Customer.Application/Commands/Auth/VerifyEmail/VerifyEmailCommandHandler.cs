using MediatR;
using PrintGrid.Modules.Customer.Application.Auth;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Commands.Auth.VerifyEmail;

public class VerifyEmailCommandHandler(
    ICustomerRepository customers,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher)
    : IRequestHandler<VerifyEmailCommand, Result>
{
    public async Task<Result> Handle(VerifyEmailCommand command, CancellationToken cancellationToken)
    {
        var customer = await customers.GetByEmailAsync(command.Email, cancellationToken);
        if (customer is null)
            return Result.Failure(new Error("invalid_token", "Invalid token or email"));

        if (customer.IsEmailVerified)
            return Result.Failure(new Error("already_verified", "Email is already verified"));

        if (customer.VerificationTokenExpiresAt < DateTime.UtcNow)
            return Result.Failure(new Error("token_expired", "Token has expired"));

        if (customer.VerificationTokenHash is null || !passwordHasher.Verify(command.Token, customer.VerificationTokenHash))
            return Result.Failure(new Error("invalid_token", "Invalid token"));

        customer.VerifyEmail();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
