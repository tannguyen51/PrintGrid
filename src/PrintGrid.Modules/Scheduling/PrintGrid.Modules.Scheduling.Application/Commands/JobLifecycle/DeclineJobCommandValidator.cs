using FluentValidation;

namespace PrintGrid.Modules.Scheduling.Application.Commands.JobLifecycle;

public class DeclineJobCommandValidator : AbstractValidator<DeclineJobCommand>
{
    public DeclineJobCommandValidator()
    {
        RuleFor(c => c.JobId).NotEmpty();
        RuleFor(c => c.Reason).NotEmpty().MaximumLength(500);
    }
}
