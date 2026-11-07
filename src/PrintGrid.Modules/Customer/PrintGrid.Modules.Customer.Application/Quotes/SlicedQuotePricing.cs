namespace PrintGrid.Modules.Customer.Application.Quotes;

public sealed record QuoteColorUsage(string ColorCode, decimal Grams);
public sealed record QuoteColorPrice(decimal Amount);
public sealed record QuoteSliceRequest(string StorageKey, string FileFormat, decimal Scale,
    string MaterialCode, IReadOnlyList<string> Colors, decimal Density, decimal LayerHeightMm,
    int InfillPercent, int Quantity, Guid MachineId);
public sealed record QuoteSliceResult(IReadOnlyList<QuoteColorUsage> Colors, decimal PrintMinutes, decimal? PricePerHour = null, decimal? FailureRate = null);

public interface IQuoteSlicer
{
    Task<IReadOnlyList<Guid>> GetConfiguredMachineIdsAsync(CancellationToken cancellationToken);
    Task<QuoteSliceResult> SliceAsync(QuoteSliceRequest request, CancellationToken cancellationToken);
}

/// <summary>Totals for the entire batch, including support/purge already reported by the slicer.</summary>
public sealed record QuotePriceBreakdown(
    IReadOnlyList<QuoteColorUsage> Colors,
    decimal PrintMinutes,
    decimal MaterialAmount,
    decimal MachineAmount,
    decimal ServiceAmount,
    decimal FailureAllowance,
    decimal TotalAmount,
    decimal WidthMm,
    decimal DepthMm,
    decimal HeightMm);

public static class SlicedQuotePricing
{
    public static decimal ScaleFor(string? size, decimal width, decimal depth, decimal height)
    {
        if (width <= 0 || depth <= 0 || height <= 0) throw new ArgumentException("Kích thước file không hợp lệ.");
        var target = size?.ToUpperInvariant() switch
        {
            null or "ORIGINAL" => 0m,
            "S" => 100m,
            "M" => 200m,
            "L" => 300m,
            _ => throw new ArgumentException("Size phải là S, M, L hoặc ORIGINAL.")
        };
        return target == 0 ? 1m : target / Math.Max(width, Math.Max(depth, height));
    }

    public static QuotePriceBreakdown Calculate(PricingParameterSet rates, string material, int quantity,
        QuoteSliceResult slice, bool polishing, decimal widthMm = 0, decimal depthMm = 0, decimal heightMm = 0)
    {
        if (quantity is < 1 or > 100 || slice.PrintMinutes <= 0 || slice.Colors.Count == 0)
            throw new ArgumentException("Kết quả slicer hoặc số lượng không hợp lệ.");
        var failure = slice.FailureRate ?? rates.FailureRate;
        var hourly = slice.PricePerHour ?? rates.PricePerPrintHour;
        if (failure is < 0 or >= 1 || hourly <= 0) throw new ArgumentException("Đơn giá máy hoặc tỷ lệ lỗi không hợp lệ.");
        var materialAmount = slice.Colors.Sum(c =>
        {
            if (c.Grams <= 0) throw new ArgumentException("Slicer trả khối lượng màu không hợp lệ.");
            var key = $"{material.ToUpperInvariant()}:{c.ColorCode.ToUpperInvariant()}";
            var perKg = rates.ColorPricePerKg is not null && rates.ColorPricePerKg.TryGetValue(key, out var overrideRate)
                ? overrideRate : (rates.PricePerGram.TryGetValue(material, out var perGram) ? perGram * 1000m
                    : throw new ArgumentException("Chưa có đơn giá vật liệu."));
            return perKg <= 0 ? throw new ArgumentException("Đơn giá màu không hợp lệ.") : c.Grams / 1000m * perKg;
        });
        var machine = slice.PrintMinutes / 60m * hourly;
        var polishingRate = polishing ? rates.PolishingPerUnit : 0m;
        if (polishingRate < 0) throw new ArgumentException("Đơn giá đánh bóng không hợp lệ.");
        var service = quantity * polishingRate;
        var failureAllowance = (materialAmount + machine) * failure / (1 - failure);
        var total = decimal.Round(materialAmount + machine + service + failureAllowance, 0, MidpointRounding.AwayFromZero);
        return new QuotePriceBreakdown(slice.Colors, slice.PrintMinutes, materialAmount, machine, service,
            failureAllowance, total, widthMm, depthMm, heightMm);
    }

    /// <summary>Section 4 BR-05: Calculate quoted price for a pinned model, applying surcharge coefficient.</summary>
    public static QuotePriceBreakdown CalculateWithPinSurcharge(PricingParameterSet rates, string material, int quantity,
        QuoteSliceResult slice, bool polishing, decimal pinCoefficient, decimal widthMm = 0, decimal depthMm = 0, decimal heightMm = 0)
    {
        // Use unrounded total before surcharge, then multiply by h_chỉ_định as per doc formula
        var baseTotal = decimal.Round(slice.Colors.Sum(c =>
        {
            var key = $"{material.ToUpperInvariant()}:{c.ColorCode.ToUpperInvariant()}";
            var perKg = rates.ColorPricePerKg != null && rates.ColorPricePerKg.TryGetValue(key, out var r) ? r
                : (rates.PricePerGram.TryGetValue(material, out var pg) ? pg * 1000m : 0m);
            return c.Grams / 1000m * perKg;
        }), 2, MidpointRounding.AwayFromZero);

        var hourly = slice.PricePerHour ?? rates.PricePerPrintHour;
        var machineCost = slice.PrintMinutes / 60m * hourly;
        var serviceCost = polishing ? quantity * rates.PolishingPerUnit : 0m;
        var failureRate = slice.FailureRate ?? rates.FailureRate;
        var allowance = (baseTotal + machineCost) * failureRate / (1 - failureRate);

        var subtotal = baseTotal + machineCost + serviceCost + allowance;
        var withSurcharge = Math.Round(subtotal * pinCoefficient, 0, MidpointRounding.AwayFromZero);

        return new QuotePriceBreakdown(slice.Colors, slice.PrintMinutes, baseTotal, machineCost, serviceCost, allowance,
            withSurcharge, widthMm, depthMm, heightMm);
    }
}
