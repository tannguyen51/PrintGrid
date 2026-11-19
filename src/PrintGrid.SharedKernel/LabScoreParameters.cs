namespace PrintGrid.SharedKernel;

/// <summary>
/// Parameters that control how provisional labs are scored.  Stored in the active commercial
/// configuration so Ops has one place to tune them.
/// </summary>
public sealed record LabScoreParameters(
    decimal ProvisionalBaseScore = 0.5m,
    int ProvisionalWeightK = 5,
    int ProvisionalThreshold = 10);
