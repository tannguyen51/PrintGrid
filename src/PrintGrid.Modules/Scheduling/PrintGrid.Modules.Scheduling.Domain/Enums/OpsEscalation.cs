namespace PrintGrid.Modules.Scheduling.Domain.Enums;

/// <summary>Why a job was pushed to the operations queue (FR-ANAL-004 / BR-RESCHED-004).</summary>
public enum OpsEscalationKind
{
    /// <summary>Reprint cap reached — auto-reprint stops, a human decides (BR-RESCHED-004, B14 chốt 23/09).</summary>
    ReprintLimitExceeded,

    /// <summary>No feasible slot before the committed date — needs a human + the customer (BR-SCHED-011).</summary>
    NoFeasibleSlot
}

public enum OpsEscalationStatus
{
    Open,
    Resolved
}
