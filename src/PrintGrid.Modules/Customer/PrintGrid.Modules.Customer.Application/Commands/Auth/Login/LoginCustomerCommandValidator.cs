using FluentValidation;

namespace PrintGrid.Modules.Customer.Application.Commands.Auth.Login;

public class LoginCustomerCommandValidator : AbstractValidator<LoginCustomerCommand>
{
    public LoginCustomerCommandValidator()
    {
        RuleFor(c => c.Email).NotEmpty().MaximumLength(256).EmailAddress();
        RuleFor(c => c.Password).NotEmpty();
    }
}