using FluentValidation;

namespace PrintGrid.Modules.Scheduling.Application.Commands.JobLifecycle;

public class AcceptJobCommandValidator : AbstractValidator<AcceptJobCommand>
{
    public AcceptJobCommandValidator() => RuleFor(c => c.JobId).NotEmpty();
}