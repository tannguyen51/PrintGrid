namespace PrintGrid.Modules.Customer.Application.Quotes;

/// <summary>
/// Network-uniform pricing FORMULA (FR-SCHED-010, BR-QUOTE-001). The formula is fixed;
/// every rate comes from a versioned <see cref="PricingParameterSet"/> that the quote
/// freezes at creation time (BR-QUOTE-004). Repricing a frozen quote means re-running
/// this class with <see cref="PricingParameterSet.ByVersion"/> — deterministic.
/// </summary>
public static class QuotePricing
{
    public sealed record PricedLine(
        decimal UnitPrice,
        decimal MaterialCostAmount,
        decimal MachineTimeCostAmount,
        int EffectiveMinutes,
        decimal Grams);

    public static bool IsSupported(PricingParameterSet set, string materialCode) =>
        set.PricePerGram.ContainsKey(materialCode);

    public static decimal MaterialGrams(PricingParameterSet set, decimal volumeCm3, decimal infillPercent, string materialCode)
    {
        // Infill fractional mass between ~30% (hollow-ish) and 100% solid.
        var density = set.DensityGramsPerCm3.TryGetValue(materialCode, out var d) ? d : 1.2m;
        var infillFactor = 0.3m + (infillPercent / 100m) * 0.7m;
        return decimal.Round(volumeCm3 * density * infillFactor, 1);
    }

    public static decimal MaterialCost(PricingParameterSet set, string materialCode, decimal grams) =>
        (set.PricePerGram.TryGetValue(materialCode, out var p) ? p : set.PricePerGram["PLA"]) * grams;

    public static decimal MachineTimeCost(PricingParameterSet set, int effectiveMinutes) =>
        effectiveMinutes / 60m * set.PricePerPrintHour;

    public static int EffectiveMinutes(PricingParameterSet set, int baseMinutes, decimal layerHeightMm) =>
        (int)decimal.Round(baseMinutes * (set.TimeMultiplierByLayer.TryGetValue(layerHeightMm, out var m) ? m : 1.25m));

    /// <summary>Prices one unit end-to-end. Same inputs + same set version ⇒ same output (AC of FR-SCHED-010).</summary>
    public static PricedLine PriceLine(
        PricingParameterSet set,
        decimal volumeCm3,
        int baseMinutes,
        decimal infillPercent,
        string materialCode,
        decimal layerHeightMm)
    {
        var effective = EffectiveMinutes(set, baseMinutes, layerHeightMm);
        var grams = MaterialGrams(set, volumeCm3, infillPercent, materialCode);
        var material = MaterialCost(set, materialCode, grams);
        var machine = MachineTimeCost(set, effective);
        return new PricedLine(
            decimal.Round(material + machine, 0),
            decimal.Round(material, 0),
            decimal.Round(machine, 0),
            effective,
            grams);
    }
}
