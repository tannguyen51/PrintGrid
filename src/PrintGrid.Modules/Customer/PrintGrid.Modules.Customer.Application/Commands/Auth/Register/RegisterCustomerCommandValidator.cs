using FluentValidation;

namespace PrintGrid.Modules.Customer.Application.Commands.Auth.Register;

public class RegisterCustomerCommandValidator : AbstractValidator<RegisterCustomerCommand>
{
    public RegisterCustomerCommandValidator()
    {
        RuleFor(c => c.FullName).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Email).NotEmpty().MaximumLength(256).EmailAddress();
        RuleFor(c => c.Password).NotEmpty().MinimumLength(6);
        RuleFor(c => c.PhoneNumber).MaximumLength(32);
    }
}