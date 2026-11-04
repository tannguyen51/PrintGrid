namespace PrintGrid.Modules.Customer.Application.Quotes;

public sealed record QuoteColorUsage(string ColorCode, decimal Grams);
public sealed record QuoteColorPrice(string ColorCode, decimal Grams, decimal PricePerKg, decimal Amount);
public sealed record QuoteSliceRequest(string StorageKey, string FileFormat, decimal Scale,
    string MaterialCode, IReadOnlyList<string> Colors, decimal Density, decimal LayerHeightMm,
    int InfillPercent, int Quantity, Guid MachineId);
public sealed record QuoteSliceResult(IReadOnlyList<QuoteColorUsage> Colors, decimal PrintMinutes,
    string Engine, decimal? PricePerHour = null, decimal? FailureRate = null);

public interface IQuoteSlicer
{
    Task<IReadOnlyList<Guid>> GetConfiguredMachineIdsAsync(CancellationToken cancellationToken);
    Task<QuoteSliceResult> SliceAsync(QuoteSliceRequest request, CancellationToken cancellationToken);
}

/// <summary>Totals for the entire batch, including support/purge already reported by the slicer.</summary>
public sealed record QuotePriceBreakdown(string SizeCode, decimal WidthMm, decimal DepthMm, decimal HeightMm,
    decimal Scale, int Quantity, string Engine, IReadOnlyList<QuoteColorPrice> Colors,
    decimal PrintMinutes, decimal PricePerHour, decimal MaterialAmount, decimal MachineAmount,
    decimal FailureRate, decimal FailureAllowance, decimal PolishingPerUnit, decimal ServiceAmount,
    decimal TotalAmount);

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

    public static QuotePriceBreakdown Calculate(PricingParameterSet rates, string material,
        string size, decimal width, decimal depth, decimal height, decimal scale, int quantity,
        QuoteSliceResult slice, bool polishing)
    {
        if (quantity is < 1 or > 100 || slice.PrintMinutes <= 0 || slice.Colors.Count == 0)
            throw new ArgumentException("Kết quả slicer hoặc số lượng không hợp lệ.");
        var failure = slice.FailureRate ?? rates.FailureRate;
        var hourly = slice.PricePerHour ?? rates.PricePerPrintHour;
        if (failure is < 0 or >= 1 || hourly <= 0) throw new ArgumentException("Đơn giá máy hoặc tỷ lệ lỗi không hợp lệ.");
        var colors = slice.Colors.Select(c =>
        {
            if (c.Grams <= 0) throw new ArgumentException("Slicer trả khối lượng màu không hợp lệ.");
            var key = $"{material.ToUpperInvariant()}:{c.ColorCode.ToUpperInvariant()}";
            var perKg = rates.ColorPricePerKg is not null && rates.ColorPricePerKg.TryGetValue(key, out var overrideRate)
                ? overrideRate : (rates.PricePerGram.TryGetValue(material, out var perGram) ? perGram * 1000m
                    : throw new ArgumentException("Chưa có đơn giá vật liệu."));
            if (perKg <= 0) throw new ArgumentException("Đơn giá màu không hợp lệ.");
            return new QuoteColorPrice(c.ColorCode, c.Grams, perKg, c.Grams / 1000m * perKg);
        }).ToList();
        var materialAmount = colors.Sum(c => c.Amount);
        var machine = slice.PrintMinutes / 60m * hourly;
        var printing = (materialAmount + machine) / (1 - failure);
        var polishingRate = polishing ? rates.PolishingPerUnit : 0m;
        if (polishingRate < 0) throw new ArgumentException("Đơn giá đánh bóng không hợp lệ.");
        var service = quantity * polishingRate;
        return new(size, width * scale, depth * scale, height * scale, scale, quantity,
            slice.Engine, colors, slice.PrintMinutes, hourly, materialAmount, machine, failure,
            printing - materialAmount - machine, polishingRate, service,
            decimal.Round(printing + service, 0, MidpointRounding.AwayFromZero));
    }
}
