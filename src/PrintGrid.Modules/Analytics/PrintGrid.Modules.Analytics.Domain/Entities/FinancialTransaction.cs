using PrintGrid.SharedKernel.Common;

namespace PrintGrid.Modules.Analytics.Domain.Entities;

public sealed class FinancialTransaction : Entity<Guid>
{
    private FinancialTransaction() { }

    public Guid OrderId { get; private set; }
    public Guid? RefundRequestId { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "VND";
    public string Reference { get; private set; } = string.Empty;
    public string IdempotencyKey { get; private set; } = string.Empty;
    public Guid ConfirmedBy { get; private set; }
    public DateTime ConfirmedAtUtc { get; private set; }
    public string Status { get; private set; } = "Confirmed";
    public string? Note { get; private set; }

    public static FinancialTransaction Create(Guid orderId, string type, decimal amount, string currency,
        string reference, string idempotencyKey, Guid confirmedBy, DateTime confirmedAtUtc,
        Guid? refundRequestId = null, string? note = null) => new()
    {
        Id = Guid.NewGuid(), OrderId = orderId, RefundRequestId = refundRequestId,
        Type = type, Amount = amount, Currency = currency, Reference = reference,
        IdempotencyKey = idempotencyKey, ConfirmedBy = confirmedBy,
        ConfirmedAtUtc = confirmedAtUtc, Note = note?.Trim()
    };
}
