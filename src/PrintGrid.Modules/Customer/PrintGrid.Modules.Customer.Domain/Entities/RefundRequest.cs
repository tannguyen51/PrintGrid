using PrintGrid.SharedKernel.Common;

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
}
