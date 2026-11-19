namespace PrintGrid.Modules.Scheduling.Domain.Services;

/// <summary>
/// Score snapshot for a single lab at the moment of assignment.  The values are normalized to
/// the 0..1 range and the lab's provisional/temporary status is explicit.
/// </summary>
public sealed record LabScoreSnapshot(
    Guid LabId,
    decimal OnTimeRate,
    decimal FirstPassYield,
    decimal UtilizationRate,
    decimal CostRate,
    decimal TransitRate,
    bool IsProvisional,
    int CompletedJobs);
