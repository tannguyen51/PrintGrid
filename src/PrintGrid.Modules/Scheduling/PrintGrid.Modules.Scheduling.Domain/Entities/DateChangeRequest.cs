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

    public DateChangeReason ReasonCode { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public string Token { get; private set; } = string.Empty;
    public DateChangeRequestStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? DecidedAtUtc { get; private set; }
    public Guid? DecidedByCustomerId { get; private set; }

    private DateChangeRequest() { }

    public static DateChangeRequest Propose(
        Guid jobId,
        Guid orderItemId,
        DateOnly originalDeliveryDate,
        DateOnly proposedDeliveryDate,
        DateChangeReason reasonCode,
        string reason,
        string token,
        DateTime? createdAtUtc = null)
    {
        if (!Enum.IsDefined(reasonCode))
            throw new ArgumentOutOfRangeException(nameof(reasonCode), "Only the three B16 reasons are allowed");
        if (proposedDeliveryDate <= originalDeliveryDate)
            throw new ArgumentOutOfRangeException(nameof(proposedDeliveryDate), "The proposed date must be later than the committed date");
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("A date-change explanation is required", nameof(reason));
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("A customer decision token is required", nameof(token));

        return new DateChangeRequest
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            OrderItemId = orderItemId,
            OriginalDeliveryDate = originalDeliveryDate,
            ProposedDeliveryDate = proposedDeliveryDate,
            ReasonCode = reasonCode,
            Reason = reason,
            Token = token,
            Status = DateChangeRequestStatus.Proposed,
            CreatedAtUtc = createdAtUtc ?? DateTime.UtcNow
        };
    }

    public void Approve(Guid customerId, DateTime decidedAtUtc)
    {
        if (Status != DateChangeRequestStatus.Proposed)
            throw new InvalidOperationException("Only a proposed date change can be approved");
        Status = DateChangeRequestStatus.Approved;
        DecidedByCustomerId = customerId;
        DecidedAtUtc = decidedAtUtc;
    }

    public void Reject(Guid customerId, DateTime decidedAtUtc)
    {
        if (Status != DateChangeRequestStatus.Proposed)
            throw new InvalidOperationException("Only a proposed date change can be rejected");
        Status = DateChangeRequestStatus.Rejected;
        DecidedByCustomerId = customerId;
        DecidedAtUtc = decidedAtUtc;
    }
}
