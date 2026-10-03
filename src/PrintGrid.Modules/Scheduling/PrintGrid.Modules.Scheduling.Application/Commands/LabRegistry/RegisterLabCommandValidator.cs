using FluentValidation;

namespace PrintGrid.Modules.Scheduling.Application.Commands.LabRegistry;

public class RegisterLabCommandValidator : AbstractValidator<RegisterLabCommand>
{
    public RegisterLabCommandValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(200);
        RuleFor(c => c.City).NotEmpty().MaximumLength(100);
        RuleFor(c => c.TransitDaysToHub).GreaterThanOrEqualTo(0).LessThanOrEqualTo(30);
    }
}