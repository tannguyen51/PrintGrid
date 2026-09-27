using FluentValidation;

namespace PrintGrid.Modules.Customer.Application.Quotes;

public class CreateQuoteCommandValidator : AbstractValidator<CreateQuoteCommand>
{
    public CreateQuoteCommandValidator()
    {
        RuleFor(c => c.CustomerId).NotEmpty();
        RuleFor(c => c.ModelId).NotEmpty();
        RuleFor(c => c.MaterialCode).NotEmpty().MaximumLength(16);
        RuleFor(c => c.ColorCode).NotEmpty().MaximumLength(32);
        RuleFor(c => c.LayerHeightMm).InclusiveBetween(0.05m, 0.4m);
        RuleFor(c => c.InfillPercent).InclusiveBetween(0, 100);
        RuleFor(c => c.Quantity).InclusiveBetween(1, 100);
        RuleFor(c => c.ToleranceMm).GreaterThan(0).LessThanOrEqualTo(1);
    }
}