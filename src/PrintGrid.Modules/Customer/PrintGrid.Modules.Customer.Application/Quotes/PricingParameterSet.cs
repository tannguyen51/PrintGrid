namespace PrintGrid.Modules.Customer.Application.Quotes;

/// <summary>
/// A named, immutable version of the network-uniform pricing parameters (FR-SCHED-010 /
/// BR-QUOTE-004 / BR-CONFIG-001). Quotes freeze <see cref="Version"/> plus the resulting
/// per-unit cost split, so a quote can always be re-derived with the exact rates it was made
/// with — changing the active set never mutates history (BR-CONFIG-003: forward-only).
/// </summary>
public sealed record PricingParameterSet(
    string Version,
    IReadOnlyDictionary<string, decimal> PricePerGram,
    IReadOnlyDictionary<string, decimal> DensityGramsPerCm3,
    decimal PricePerPrintHour,
    IReadOnlyDictionary<decimal, decimal> TimeMultiplierByLayer)
{
    /// <summary>Rates as they stood when the register was approved (legacy constants).</summary>
    public static readonly PricingParameterSet V2026_09 = new(
        "2026.09",
        new Dictionary<string, decimal> { ["PLA"] = 450m, ["PETG"] = 550m, ["ABS"] = 500m, ["TPU"] = 650m, ["RESIN"] = 1500m },
        new Dictionary<string, decimal> { ["PLA"] = 1.24m, ["PETG"] = 1.27m, ["ABS"] = 1.04m, ["TPU"] = 1.21m, ["RESIN"] = 1.10m },
        90_000m,
        new Dictionary<decimal, decimal> { [0.3m] = 1.0m, [0.2m] = 1.25m, [0.1m] = 1.8m, [0.05m] = 2.6m });

    /// <summary>First versioned set — filament/hour rates raised ~5% (demo of forward-only change).</summary>
    public static readonly PricingParameterSet V2026_10 = new(
        "2026.10",
        new Dictionary<string, decimal> { ["PLA"] = 470m, ["PETG"] = 575m, ["ABS"] = 520m, ["TPU"] = 680m, ["RESIN"] = 1550m },
        V2026_09.DensityGramsPerCm3,
        94_500m,
        V2026_09.TimeMultiplierByLayer);

    /// <summary>Append-only history; newest entry is what new quotes use (FR-ADMIN-002 will extend this into DB config).</summary>
    public static IReadOnlyList<PricingParameterSet> All { get; } = [V2026_09, V2026_10];

    public static PricingParameterSet Active => All[^1];

    /// <summary>Resolves a frozen version for recomputation; falls back to Active for legacy/blank rows.</summary>
    public static PricingParameterSet ByVersion(string? version) =>
        All.FirstOrDefault(s => s.Version == version) ?? Active;
}
