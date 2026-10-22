using FluentValidation;

namespace PrintGrid.Modules.Customer.Application.Commands.Auth.ResetPassword;

public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Token).NotEmpty().MaximumLength(256);
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8).MaximumLength(128);
    }
}
