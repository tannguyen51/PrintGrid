using MediatR;
using PrintGrid.Modules.Customer.Application.Auth;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Commands.Auth.VerifyEmail;

public class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand, Result>
{
    private readonly ICustomerRepository _customers;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;

    public VerifyEmailCommandHandler(
        ICustomerRepository customers,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher)
    {
        _customers = customers;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result> Handle(VerifyEmailCommand command, CancellationToken cancellationToken)
    {
        var customer = await _customers.GetByEmailAsync(command.Email, cancellationToken);
        if (customer is null)
            return Result.Failure(new Error("invalid_token", "Invalid token or email"));

        if (customer.IsEmailVerified)
            return Result.Failure(new Error("already_verified", "Email is already verified"));

        if (customer.VerificationTokenExpiresAt < DateTime.UtcNow)
            return Result.Failure(new Error("token_expired", "Token has expired"));

        if (customer.VerificationTokenHash is null || !_passwordHasher.Verify(command.Token, customer.VerificationTokenHash))
            return Result.Failure(new Error("invalid_token", "Invalid token"));

        customer.VerifyEmail();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
