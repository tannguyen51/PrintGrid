using FluentValidation;

namespace PrintGrid.Modules.Scheduling.Application.Commands.LabRegistry;

public class RegisterMachineCommandValidator : AbstractValidator<RegisterMachineCommand>
{
    public RegisterMachineCommandValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(100);
        RuleFor(c => c.Model).NotEmpty().MaximumLength(100);
        RuleFor(c => c.BuildWidthMm).GreaterThan(0).LessThanOrEqualTo(1000);
        RuleFor(c => c.BuildDepthMm).GreaterThan(0).LessThanOrEqualTo(1000);
        RuleFor(c => c.BuildHeightMm).GreaterThan(0).LessThanOrEqualTo(1000);
        RuleFor(c => c.MinLayerHeightMm).GreaterThan(0).LessThanOrEqualTo(1);
        RuleFor(c => c.AchievableToleranceMm).GreaterThan(0).LessThanOrEqualTo(5);
        RuleFor(c => c.SupportedMaterials).NotNull().Must(m => m.Count > 0)
            .WithMessage("A machine must list at least one supported material");
    }
}