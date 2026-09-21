namespace PrintGrid.Modules.Customer.Application.Quotes;

/// <summary>
/// Network-uniform pricing (FR-QUOTE) — deterministic mock rates in VND.
/// These drive the demo quote; real pricing will flow from the Scheduling module's
/// pricing engine (reference versioned parameters in the design docs).
/// </summary>
public static class QuotePricing
{
    // VND per gram of material consumed.
    private static readonly IReadOnlyDictionary<string, decimal> PricePerGram = new Dictionary<string, decimal>
    {
        ["PLA"] = 450m,
        ["PETG"] = 550m,
        ["ABS"] = 500m,
        ["TPU"] = 650m,
        ["RESIN"] = 1500m,
    };

    private static readonly IReadOnlyDictionary<string, decimal> DensityGramsPerCm3 = new Dictionary<string, decimal>
    {
        ["PLA"] = 1.24m,
        ["PETG"] = 1.27m,
        ["ABS"] = 1.04m,
        ["TPU"] = 1.21m,
        ["RESIN"] = 1.10m,
    };

    /// <summary>Machine time rate — VND per hour (simplified).</summary>
    public const decimal PricePerPrintHour = 90000m;

    /// <summary>VND per layer-height quality step (multiplier on print time).</summary>
    public static readonly IReadOnlyDictionary<decimal, decimal> TimeMultiplierByLayer = new Dictionary<decimal, decimal>
    {
        [0.3m] = 1.0m,   // Draft
        [0.2m] = 1.25m,  // Standard
        [0.1m] = 1.8m,   // High
        [0.05m] = 2.6m,  // Ultra
    };

    public static bool IsSupported(string materialCode) => PricePerGram.ContainsKey(materialCode);

    public static decimal MaterialGrams(decimal volumeCm3, decimal infillPercent, string materialCode)
    {
        // Infill fractional mass between 15% (hollow-ish) and 100%.
        var density = DensityGramsPerCm3.TryGetValue(materialCode, out var d) ? d : 1.2m;
        var infillFactor = 0.3m + (infillPercent / 100m) * 0.7m;
        return decimal.Round(volumeCm3 * density * infillFactor, 1);
    }

    public static decimal MaterialCost(string materialCode, decimal grams) =>
        (PricePerGram.TryGetValue(materialCode, out var p) ? p : PricePerGram["PLA"]) * grams;

    public static decimal MachineTimeCost(int effectiveMinutes) => effectiveMinutes / 60m * PricePerPrintHour;

    public static int EffectiveMinutes(int baseMinutes, decimal layerHeightMm) =>
        (int)decimal.Round(baseMinutes * (TimeMultiplierByLayer.TryGetValue(layerHeightMm, out var m) ? m : 1.25m));
}