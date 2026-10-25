using FluentValidation;

namespace PrintGrid.Modules.Customer.Application.Commands.Auth.ForgotPassword;

public sealed class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator() =>
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
}
