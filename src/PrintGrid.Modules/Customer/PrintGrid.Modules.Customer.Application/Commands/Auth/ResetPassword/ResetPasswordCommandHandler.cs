using MediatR;
using PrintGrid.Modules.Customer.Application.Auth;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Commands.Auth.ResetPassword;

public sealed class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result>
{
    private readonly ICustomerRepository _customers;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;

    public ResetPasswordCommandHandler(ICustomerRepository customers, IUnitOfWork unitOfWork, IPasswordHasher passwordHasher)
    {
        _customers = customers;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result> Handle(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        var customer = await _customers.GetByEmailAsync(command.Email, cancellationToken);
        if (customer is null ||
            !customer.IsPasswordResetTokenValid(DateTime.UtcNow) ||
            customer.PasswordResetTokenHash is null ||
            !_passwordHasher.Verify(command.Token, customer.PasswordResetTokenHash))
        {
            return Result.Failure(new Error("invalid_reset_token", "Liên kết đặt lại mật khẩu không hợp lệ hoặc đã hết hạn"));
        }

        customer.ResetPassword(_passwordHasher.Hash(command.NewPassword.Trim()));
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
