using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Customer.Application.Abstractions;
using PrintGrid.Modules.Customer.Domain.ValueObjects;

namespace PrintGrid.Modules.Customer.Infrastructure.Services;

/// <summary>
/// Resolves machine filter queries via EF against PrintGridDbContext.
/// For tech constraints filtering, delegates to application-level capability checks
/// because querying Machine/Scheduling entities would create cross-module dependencies.
/// </summary>
public class EfMachineFilterService : IMachineFilterService
{
    private readonly PrintGridDbContext _db;

    public EfMachineFilterService(PrintGridDbContext db) => _db = db;

    /// <summary>
    /// Tech constraints are validated at quote time by slicing engine against each machine.
    /// We simply pass through here — actual filtering happens in CreateSlicedQuote
    /// where we iterate machines and skip non-matching ones.
    /// </summary>
    public Task<IReadOnlyList<Guid>> FilterByTechConstraintsAsync(
        IReadOnlyList<Guid> candidateMachineIds,
        TechConstraints? techConstraints,
        CancellationToken ct)
    {
        // Constraints will be checked inline during slicing loop
        return Task.FromResult<IReadOnlyList<Guid>>(candidateMachineIds);
    }

    /// <summary>
    /// Pinned model returns only that machine ID if it was in candidates,
    /// plus all candidates as fallback. Actual feasibility determined during slice phase.
    /// </summary>
    public async Task<(IReadOnlyList<Guid> pinnedIds, IReadOnlyList<Guid> unpinnedFallback)> FilterByPinModelAsync(
        Guid pinnedModelId,
        IReadOnlyList<Guid> allCandidateIds,
        CancellationToken ct)
    {
        IReadOnlyList<Guid> pinned;
        if (allCandidateIds.Contains(pinnedModelId))
            pinned = new List<Guid> { pinnedModelId }.AsReadOnly();
        else
            pinned = Array.Empty<Guid>().ToList().AsReadOnly();
        return (pinned, allCandidateIds);
    }

    /// <summary>
    /// Returns model IDs where approved machine count meets BR-04 thresholds.
    /// Simplified: returns empty list since we don't have access to Machine entity schema here.
    /// TODO: Move machine capability queries to a shared module or CQRS query.
    /// </summary>
    public Task<IReadOnlyList<Guid>> GetEligiblePinModelsAsync(
        string materialCode, int maxColors, bool? enclosedChamber,
        decimal? nozzleDiameterMm, decimal? maxToleranceMm, decimal? minLayerHeightMm,
        int minMachines = 3, int minLabs = 2, CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyList<Guid>>([]);
    }
}
