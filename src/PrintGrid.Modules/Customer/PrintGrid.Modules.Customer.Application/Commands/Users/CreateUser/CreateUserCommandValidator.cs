using FluentValidation;
using PrintGrid.Modules.Customer.Application.Users;

namespace PrintGrid.Modules.Customer.Application.Commands.Users.CreateUser;

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(128);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PhoneNumber).MaximumLength(32);
        RuleFor(x => x.Role).Must(UserRoleCatalog.IsKnown)
            .WithMessage("Role không hợp lệ.");
    }
}
