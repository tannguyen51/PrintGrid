using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.SharedKernel.Common;

namespace PrintGrid.Modules.Scheduling.Domain.Entities;

/// <summary>
/// "Xin dời" flow (BR-SCHED-008 / BR-SCHED-011 / BR-NOTIFY-002): when the committed delivery
/// date can no longer be met, the platform states the earliest date it CAN meet and asks the
/// customer to approve — it is never late silently. The <see cref="Token"/> is what the
/// customer-facing approval link (POST /date-change-requests/{token}/approve, T4) carries.
/// </summary>
public class DateChangeRequest : AggregateRoot<Guid>
{
    public Guid JobId { get; private set; }
    public Guid OrderItemId { get; private set; }

    /// <summary>Delivery date originally committed to the customer.</summary>
    public DateOnly OriginalDeliveryDate { get; private set; }

    /// <summary>Earliest date the network can actually deliver, proposed to the customer.</summary>
    public DateOnly ProposedDeliveryDate { get; private set; }

    public string Reason { get; private set; } = string.Empty;
    public string Token { get; private set; } = string.Empty;
    public DateChangeRequestStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? DecidedAtUtc { get; private set; }

    private DateChangeRequest() { }

    public static DateChangeRequest Propose(
        Guid jobId,
        Guid orderItemId,
        DateOnly originalDeliveryDate,
        DateOnly proposedDeliveryDate,
        string reason,
        string token,
        DateTime? createdAtUtc = null)
    {
        return new DateChangeRequest
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            OrderItemId = orderItemId,
            OriginalDeliveryDate = originalDeliveryDate,
            ProposedDeliveryDate = proposedDeliveryDate,
            Reason = reason,
            Token = token,
            Status = DateChangeRequestStatus.Proposed,
            CreatedAtUtc = createdAtUtc ?? DateTime.UtcNow
        };
    }

    public void Approve(DateTime decidedAtUtc)
    {
        Status = DateChangeRequestStatus.Approved;
        DecidedAtUtc = decidedAtUtc;
    }

    public void Reject(DateTime decidedAtUtc)
    {
        Status = DateChangeRequestStatus.Rejected;
        DecidedAtUtc = decidedAtUtc;
    }
}
