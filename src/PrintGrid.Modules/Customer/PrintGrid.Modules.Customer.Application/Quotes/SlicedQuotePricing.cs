using PrintGrid.SharedKernel.ValueObjects;

namespace PrintGrid.Modules.Customer.Application.Quotes;

public sealed record QuoteColorUsage(string ColorCode, decimal Grams, decimal? PricePerKg = null, decimal? Amount = null);
public sealed record QuoteColorPrice(decimal Amount);
public sealed record QuoteModelEnvelope(decimal WidthMm, decimal DepthMm, decimal HeightMm, int SourcePlateCount = 1);
public sealed record QuoteSliceRequest(string StorageKey, string FileFormat, decimal Scale,
    string MaterialCode, IReadOnlyList<string> Colors, decimal Density, decimal LayerHeightMm,
    int InfillPercent, int Quantity, Guid MachineId, Guid? ProfileId = null,
    IReadOnlyList<ColorGroupAssignment>? ColorAssignments = null);
public sealed record QuoteSliceResult(IReadOnlyList<QuoteColorUsage> Colors, decimal PrintMinutes,
    decimal? PricePerHour = null, decimal? FailureRate = null, int PlateCount = 1,
    Guid? ProfileId = null, string? ProfileVersion = null,
    IReadOnlyList<ColorGroupAssignment>? ColorAssignments = null,
    string? SlicingEngine = null, string? SlicingProfileHash = null, decimal ModelScale = 1m, int SourcePlateCount = 1,
    int? ToolChangeCount = null, decimal? MinimumBuildWidthMm = null);

public interface IQuoteSlicer
{
    Task<IReadOnlyList<Guid>> GetConfiguredMachineIdsAsync(CancellationToken cancellationToken);
    Task<QuoteSliceResult> SliceAsync(QuoteSliceRequest request, CancellationToken cancellationToken);
    Task<QuoteModelEnvelope?> GetEnvelopeAsync(string storageKey, string format, CancellationToken cancellationToken)
        => Task.FromResult<QuoteModelEnvelope?>(null);
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
    decimal HeightMm,
    int Quantity = 1,
    decimal SetupAmount = 0,
    decimal HubAmount = 0,
    decimal PinSurchargeAmount = 0,
    decimal PinSurchargeCoefficient = 1,
    decimal CalibrationFactor = 1,
    decimal SlicerPrintMinutes = 0,
    int PlateCount = 1,
    Guid? ProfileId = null,
    string? ProfileVersion = null,
    ProductionRequirements? Requirements = null,
    PricingParameterSet? PricingParameters = null,
    decimal PriorityCoefficient = 1,
    string? FormulaVersion = null,
    IReadOnlyList<ColorGroupAssignment>? ColorAssignments = null,
    string? SlicingEngine = null, string? SlicingProfileHash = null, decimal ModelScale = 1m, int SourcePlateCount = 1,
    int? ToolChangeCount = null);

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
        QuoteSliceResult slice, bool polishing, decimal widthMm = 0, decimal depthMm = 0, decimal heightMm = 0,
        decimal calibrationFactor = 1m, decimal pinCoefficient = 1m, decimal priorityCoefficient = 1m)
    {
        if (quantity is < 1 or > 100 || slice.PrintMinutes <= 0 || slice.Colors.Count == 0)
            throw new ArgumentException("Kết quả slicer hoặc số lượng không hợp lệ.");
        // Selling rates are network-wide. A lab's contractual rates must never change a customer quote.
        var failure = rates.FailureRate;
        var hourly = rates.PricePerPrintHour;
        if (failure is < 0 or >= 1 || hourly <= 0 || calibrationFactor <= 0 || pinCoefficient < 1 || priorityCoefficient < 1
            || rates.MaterialWasteRate is < 0 or > 1 || rates.FailureCompletionRatio is < 0 or > 1
            || rates.SetupFeePerPlate < 0 || rates.HubHandlingPerUnit < 0 || rates.RoundingIncrement <= 0 || slice.PlateCount < 1)
            throw new ArgumentException("Tham số tính giá không hợp lệ.");
        var colors = slice.Colors.Select(c =>
        {
            if (c.Grams <= 0) throw new ArgumentException("Slicer trả khối lượng màu không hợp lệ.");
            var key = $"{material.ToUpperInvariant()}:{c.ColorCode.ToUpperInvariant()}";
            var perKg = rates.ColorPricePerKg is not null && rates.ColorPricePerKg.TryGetValue(key, out var overrideRate)
                ? overrideRate : (rates.PricePerGram.TryGetValue(material, out var perGram) ? perGram * 1000m
                    : throw new ArgumentException("Chưa có đơn giá vật liệu."));
            if (perKg <= 0) throw new ArgumentException("Đơn giá màu không hợp lệ.");
            return c with { PricePerKg = perKg, Amount = c.Grams / 1000m * perKg * (1 + rates.MaterialWasteRate) };
        }).ToList();
        var materialAmount = colors.Sum(c => c.Amount!.Value);
        var calibratedMinutes = slice.PrintMinutes * calibrationFactor;
        var machine = calibratedMinutes / 60m * hourly;
        var polishingRate = polishing ? rates.PolishingPerUnit : 0m;
        if (polishingRate < 0) throw new ArgumentException("Đơn giá đánh bóng không hợp lệ.");
        var service = quantity * polishingRate;
        var setup = slice.PlateCount * rates.SetupFeePerPlate;
        var hub = quantity * rates.HubHandlingPerUnit;
        var failureAllowance = (materialAmount + machine + setup) * failure * rates.FailureCompletionRatio / (1 - failure);
        var subtotal = (materialAmount + machine + setup + service + hub + failureAllowance) * priorityCoefficient;
        var surcharge = subtotal * (pinCoefficient - 1);
        var total = decimal.Round((subtotal + surcharge) / rates.RoundingIncrement, 0,
            MidpointRounding.AwayFromZero) * rates.RoundingIncrement;
        return new QuotePriceBreakdown(colors, calibratedMinutes, materialAmount, machine, service,
            failureAllowance, total, widthMm, depthMm, heightMm, quantity, setup, hub, surcharge,
            pinCoefficient, calibrationFactor, slice.PrintMinutes, slice.PlateCount, slice.ProfileId,
            slice.ProfileVersion, PricingParameters: rates, PriorityCoefficient: priorityCoefficient,
            FormulaVersion: "machine-model-v1", ColorAssignments: slice.ColorAssignments, SlicingEngine: slice.SlicingEngine,
            SlicingProfileHash: slice.SlicingProfileHash, ModelScale: slice.ModelScale, SourcePlateCount: slice.SourcePlateCount,
            ToolChangeCount: slice.ToolChangeCount);
    }

    /// <summary>Section 4 BR-05: Calculate quoted price for a pinned model, applying surcharge coefficient.</summary>
    public static QuotePriceBreakdown CalculateWithPinSurcharge(PricingParameterSet rates, string material, int quantity,
        QuoteSliceResult slice, bool polishing, decimal pinCoefficient, decimal widthMm = 0, decimal depthMm = 0, decimal heightMm = 0)
        => Calculate(rates, material, quantity, slice, polishing, widthMm, depthMm, heightMm, pinCoefficient: pinCoefficient);
}
