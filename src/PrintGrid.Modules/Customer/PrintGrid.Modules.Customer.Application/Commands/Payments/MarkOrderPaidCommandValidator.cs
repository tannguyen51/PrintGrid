using FluentValidation;

namespace PrintGrid.Modules.Customer.Application.Commands.Payments;

public sealed class MarkOrderPaidCommandValidator : AbstractValidator<MarkOrderPaidCommand>
{
    public MarkOrderPaidCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Amount)
            .GreaterThan(0m)
            .When(x => x.Amount.HasValue)
            .WithMessage("Số tiền thanh toán phải lớn hơn 0.");
    }
}
