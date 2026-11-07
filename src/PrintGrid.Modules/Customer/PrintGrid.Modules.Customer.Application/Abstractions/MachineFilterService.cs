using PrintGrid.Modules.Customer.Domain.ValueObjects;

namespace PrintGrid.Modules.Customer.Application.Abstractions;

/// <summary>
/// Filters machines based on technical constraints and pin-model configuration
/// (design doc sections 3–4). Resolved in Infrastructure via EF queries.
/// </summary>
public interface IMachineFilterService
{
    /// <summary>
    /// Returns machine IDs that satisfy all hard constraints. Empty list means no match.
    /// BR-03: constraints are HARD filters — no surcharge applied.
    /// </summary>
    Task<IReadOnlyList<Guid>> FilterByTechConstraintsAsync(
        IReadOnlyList<Guid> candidateMachineIds,
        TechConstraints? techConstraints,
        CancellationToken ct);

    /// <summary>
    /// When customer pins a model: return matched + fallback unpinned set.
    /// BR-04: only models with ≥N approved machines across ≥M labs qualify.
    /// BR-06: always provide both options for comparison display.
    /// </summary>
    Task<(IReadOnlyList<Guid> pinnedIds, IReadOnlyList<Guid> unpinnedFallback)> FilterByPinModelAsync(
        Guid pinnedModelId,
        IReadOnlyList<Guid> allCandidateIds,
        CancellationToken ct);

    /// <summary>
    /// Get eligible models for pin dropdown per BR-04 thresholds.
    /// Returns model IDs where count(approved_machines) ≥ minMachinesPerModel
    /// AND count(distinct_labs) ≥ minLabs.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetEligiblePinModelsAsync(
        string materialCode,
        int maxColors,
        bool? enclosedChamber,
        decimal? nozzleDiameterMm,
        decimal? maxToleranceMm,
        decimal? minLayerHeightMm,
        int minMachines = 3,
        int minLabs = 2,
        CancellationToken ct = default);
}
