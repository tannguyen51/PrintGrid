using FluentValidation;
using PrintGrid.Modules.Customer.Application.Users;

namespace PrintGrid.Modules.Customer.Application.Commands.Users.AssignRole;

public sealed class AssignRoleCommandValidator : AbstractValidator<AssignRoleCommand>
{
    public AssignRoleCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.ActingUserId).NotEmpty();
        RuleFor(x => x.Role).Must(UserRoleCatalog.IsKnown)
            .WithMessage("Role không hợp lệ.");
    }
}
