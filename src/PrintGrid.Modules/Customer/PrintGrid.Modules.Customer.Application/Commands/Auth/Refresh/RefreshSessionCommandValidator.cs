using FluentValidation;

namespace PrintGrid.Modules.Customer.Application.Commands.Auth.Refresh;

public class RefreshSessionCommandValidator : AbstractValidator<RefreshSessionCommand>
{
    public RefreshSessionCommandValidator()
    {
        RuleFor(c => c.RefreshToken).NotEmpty();
    }
}