namespace PrintGrid.Modules.Scheduling.Domain.Services;

/// <summary>
/// A named, immutable version of the assign-engine weights ("kỳ cấu hình", FR-SCHED-009 /
/// BR-CONFIG-002/003). Every decision log entry freezes <see cref="Version"/> plus the
/// resulting scores, so a past assignment can always be reconstructed with the weights that
/// were in force — changing the active set never mutates history (forward-only, BR-CONFIG-003).
/// Mirrors <c>PricingParameterSet</c> (FR-SCHED-010) for the scoring side.
/// </summary>
public sealed record ScoringParameterSet(string Version, ScoringWeights Weights)
{
    /// <summary>Weights the engine shipped with, kept as the first named version.</summary>
    public static readonly ScoringParameterSet V2026_09 = new("2026.09", ScoringWeights.Default);

    /// <summary>
    /// First version carried through the config register. The numbers deliberately match
    /// 2026.09 — this release only introduces the freeze/versioning mechanism; retuning the
    /// weights becomes a forward-only config change (FR-ADMIN-002) that new decisions pick up.
    /// </summary>
    public static readonly ScoringParameterSet V2026_10 = new("2026.10", ScoringWeights.Default);

    /// <summary>Append-only history; the newest entry is what new decisions use.</summary>
    public static IReadOnlyList<ScoringParameterSet> All { get; } = [V2026_09, V2026_10];

    public static ScoringParameterSet Active => All[^1];

    /// <summary>Resolves a frozen version; legacy/blank values fall back to the active set.</summary>
    public static ScoringParameterSet ByVersion(string? version) =>
        All.FirstOrDefault(s => s.Version == version) ?? Active;
}
