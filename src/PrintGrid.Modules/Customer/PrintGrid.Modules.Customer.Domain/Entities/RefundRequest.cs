using PrintGrid.SharedKernel.Common;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Domain.Entities;

/// <summary>A durable queue row for UC-028. Gateway execution is deliberately a later ops step.</summary>
public sealed class RefundRequest : Entity<Guid>
{
    private RefundRequest() { }

    public Guid OrderId { get; private set; }
    public Guid OrderItemId { get; private set; }
    public Guid DateChangeRequestId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public string PaymentTransactionId { get; private set; } = string.Empty;
    public string Reason { get; private set; } = string.Empty;
    public string Status { get; private set; } = "Pending";
    public DateTime CreatedAtUtc { get; private set; }

    public static RefundRequest Queue(
        Guid orderId, Guid orderItemId, Guid dateChangeRequestId,
        decimal amount, string currency, string paymentTransactionId,
        string reason, DateTime createdAtUtc) => new()
    {
        Id = Guid.NewGuid(),
        OrderId = orderId,
        OrderItemId = orderItemId,
        DateChangeRequestId = dateChangeRequestId,
        Amount = amount,
        Currency = currency,
        PaymentTransactionId = paymentTransactionId,
        Reason = reason,
        CreatedAtUtc = createdAtUtc
    };

    /// <summary>FR-ANAL-005: ops approves the queued refund. Only a Pending request can be decided.</summary>
    public Result Approve() => Decide("Approved");

    /// <summary>FR-ANAL-005: ops rejects the queued refund. Only a Pending request can be decided.</summary>
    public Result Reject() => Decide("Rejected");

    private Result Decide(string next)
    {
        if (Status != "Pending")
            return Result.Failure(Error.Conflict($"Yêu cầu hoàn tiền đã được xử lý (trạng thái hiện tại: {Status})."));

        Status = next;
        return Result.Success();
    }
}
