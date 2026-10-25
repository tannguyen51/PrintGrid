using FluentValidation;

namespace PrintGrid.Modules.Customer.Application.Commands.Auth.GoogleLogin;

public sealed class GoogleLoginCommandValidator : AbstractValidator<GoogleLoginCommand>
{
    public GoogleLoginCommandValidator() =>
        RuleFor(x => x.Credential).NotEmpty().MaximumLength(8192);
}
