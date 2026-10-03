using FluentValidation;

namespace PrintGrid.Modules.Scheduling.Application.Commands.JobLifecycle;

public class InspectJobCommandValidator : AbstractValidator<InspectJobCommand>
{
    public InspectJobCommandValidator()
    {
        RuleFor(c => c.JobId).NotEmpty();
        RuleFor(c => c.Note).MaximumLength(500);
    }
}