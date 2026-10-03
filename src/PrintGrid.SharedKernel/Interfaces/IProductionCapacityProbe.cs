namespace PrintGrid.SharedKernel.Interfaces;

/// <summary>
/// One quote line in a form the production network can trial-place:
/// real part size, process params and TOTAL work (minutes/grams already multiplied by quantity).
/// </summary>
public sealed record TrialPlacementLine(
    decimal PartWidthMm,
    decimal PartDepthMm,
    decimal PartHeightMm,
    string MaterialCode,
    string ColorCode,
    decimal LayerHeightMm,
    decimal ToleranceMm,
    int TotalMinutes,
    decimal TotalGrams);

/// <summary>Result of a speculative (trial-only, nothing committed) placement across the network.</summary>
public sealed record TrialPlacementResult(
    DateOnly PromisedDeliveryDate,
    Guid LabId,
    Guid MachineId,
    string Basis);

/// <summary>
/// Cross-module seam (FR-SCHED-005): the Scheduling module answers what the Customer
/// module may promise — earliest feasible delivery derived from REAL machine timelines,
/// including lab→hub transit and hub processing buffer. Returns null when no machine in
/// the network can fit the order (never sell the impossible, BR-QUOTE-006).
/// </summary>
public interface IProductionCapacityProbe
{
    Task<TrialPlacementResult?> FindEarliestFeasibleDeliveryAsync(
        IReadOnlyList<TrialPlacementLine> lines,
        DateTime notBeforeUtc,
        CancellationToken cancellationToken = default);
}
