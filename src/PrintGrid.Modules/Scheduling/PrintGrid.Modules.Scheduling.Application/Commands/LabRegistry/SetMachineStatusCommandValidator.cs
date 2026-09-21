using FluentValidation;

namespace PrintGrid.Modules.Scheduling.Application.Commands.LabRegistry;

public class SetMachineStatusCommandValidator : AbstractValidator<SetMachineStatusCommand>
{
    public SetMachineStatusCommandValidator()
    {
        RuleFor(c => c.Status).IsInEnum();
    }
}