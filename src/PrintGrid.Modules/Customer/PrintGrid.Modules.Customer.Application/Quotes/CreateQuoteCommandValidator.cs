using FluentValidation;

namespace PrintGrid.Modules.Customer.Application.Quotes;

public sealed class CreateQuoteCommandValidator : AbstractValidator<CreateQuoteCommand>
{
    public CreateQuoteCommandValidator()
    {
        RuleFor(c => c.Quantity).InclusiveBetween(1, 100);
        RuleFor(c => c.InfillPercent).InclusiveBetween(1, 100);
        RuleFor(c => c.LayerHeightMm).GreaterThan(0).LessThanOrEqualTo(1);
        RuleFor(c => c.ToleranceMm).GreaterThan(0).LessThanOrEqualTo(10);
        RuleFor(c => c.MaxColors).InclusiveBetween(1, 4).When(c => c.MaxColors.HasValue);
        RuleFor(c => c.NozzleDiameterMm).Must(n => n is null or .2m or .4m or .6m);
        RuleFor(c => c.MaxToleranceMm).GreaterThan(0).LessThanOrEqualTo(10).When(c => c.MaxToleranceMm.HasValue);
        RuleFor(c => c.MinLayerHeightMm).GreaterThan(0).LessThanOrEqualTo(1).When(c => c.MinLayerHeightMm.HasValue);
        RuleFor(c => c.BindingType).Must(t => t is null or 0 or 1)
            .WithMessage("Chỉ hỗ trợ hệ thống tự chọn hoặc chỉ định model máy.");
        RuleFor(c => c.PinnedModelId).NotEmpty().When(c => c.BindingType == 1);
        RuleFor(c => c.PinnedModelId).Null().When(c => c.BindingType != 1);
        RuleFor(c => c.ColorCodes).Must(colors => colors is null || colors.Count is >= 1 and <= 4
            && colors.All(c => !string.IsNullOrWhiteSpace(c)) && colors.Distinct(StringComparer.OrdinalIgnoreCase).Count() == colors.Count);
    }
}
