using FluentValidation;

namespace PrintGrid.Modules.Customer.Application.Commands.Refunds;

public sealed class ProcessRefundCommandValidator : AbstractValidator<ProcessRefundCommand>
{
    public ProcessRefundCommandValidator()
    {
        RuleFor(x => x.RefundRequestId).NotEmpty();
        RuleFor(x => x.ActorId).NotEmpty();
        RuleFor(x => x.Note).MaximumLength(500);
    }
}
