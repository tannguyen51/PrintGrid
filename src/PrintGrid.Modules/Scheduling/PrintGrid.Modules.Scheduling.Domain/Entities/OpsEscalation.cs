using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.SharedKernel.Common;

namespace PrintGrid.Modules.Scheduling.Domain.Entities;

/// <summary>
/// An item in the operations queue: something the engine refused to decide on its own
/// (BR-RESCHED-004 reprint cap, BR-SCHED-011 unattainable date). Ops picks it up from
/// GET /ops/escalations; nothing here rewrites the schedule by itself.
/// </summary>
public class OpsEscalation : AggregateRoot<Guid>
{
    public Guid JobId { get; private set; }
    public Guid OrderItemId { get; private set; }
    public OpsEscalationKind Kind { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public OpsEscalationStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ResolvedAtUtc { get; private set; }

    private OpsEscalation() { }

    public static OpsEscalation Raise(
        Guid jobId,
        Guid orderItemId,
        OpsEscalationKind kind,
        string reason,
        DateTime? createdAtUtc = null)
    {
        return new OpsEscalation
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            OrderItemId = orderItemId,
            Kind = kind,
            Reason = reason,
            Status = OpsEscalationStatus.Open,
            CreatedAtUtc = createdAtUtc ?? DateTime.UtcNow
        };
    }

    public void Resolve(DateTime resolvedAtUtc)
    {
        Status = OpsEscalationStatus.Resolved;
        ResolvedAtUtc = resolvedAtUtc;
    }
}
