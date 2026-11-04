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
        RuleFor(c => c.SizeCode).Must(s => s is null || new[] { "S", "M", "L", "ORIGINAL" }.Contains(s));
        RuleFor(c => c.ColorCodes).Must(colors => colors is null ||
            (colors.Count is >= 1 and <= 8 && colors.All(c => !string.IsNullOrWhiteSpace(c) && c.Length <= 32)
             && colors.Distinct(StringComparer.OrdinalIgnoreCase).Count() == colors.Count));
    }
}
