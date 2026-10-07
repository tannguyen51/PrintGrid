namespace PrintGrid.Modules.Scheduling.Domain.Services;

/// <summary>
/// Reprint limits (FR-HUB-003 / BR-RESCHED-004). B14 (chốt 23/09): a job may be reprinted
/// twice automatically; the third attempt stops and goes to the operations manager instead.
/// </summary>
public static class ReprintPolicy
{
    /// <summary>Maximum automatic reprints per job before escalating to a human.</summary>
    public const int MaxAutoReprints = 2;
}
