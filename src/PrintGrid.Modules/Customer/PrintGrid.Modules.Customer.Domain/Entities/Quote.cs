using PrintGrid.Modules.Customer.Domain.Enums;
using PrintGrid.Modules.Customer.Domain.ValueObjects;
using PrintGrid.SharedKernel.Common;
using PrintGrid.SharedKernel.Results;
using PrintGrid.SharedKernel.ValueObjects;

namespace PrintGrid.Modules.Customer.Domain.Entities;

public class Quote : AggregateRoot<Guid>
{
    private readonly List<QuoteItem> _items = new();

    public Guid CustomerId { get; private set; }
    public QuoteStatus Status { get; private set; }
    public Money TotalPrice { get; private set; } = Money.Zero();
    public DateOnly PromisedDeliveryDate { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public string? FailureReason { get; private set; }

    public IReadOnlyCollection<QuoteItem> Items => _items.AsReadOnly();

    public bool IsExpired(DateTime asOfUtc) => asOfUtc >= ExpiresAt;

    private Quote() { }

    public static Quote CreatePending(Guid customerId, TimeSpan validity)
    {
        var now = DateTime.UtcNow;
        return new Quote
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Status = QuoteStatus.Pending,
            CreatedAt = now,
            ExpiresAt = now.Add(validity)
        };
    }

    public QuoteItem AddItem(
        Guid modelId,
        int quantity,
        PrintConfiguration configuration,
        Money unitPrice,
        int estimatedPrintMinutes,
        decimal estimatedMaterialGrams)
    {
        var item = QuoteItem.Create(
            Id, modelId, quantity, configuration, unitPrice, estimatedPrintMinutes, estimatedMaterialGrams);
        _items.Add(item);
        return item;
    }

    public Result MarkReady(DateOnly promisedDeliveryDate)
    {
        if (Status != QuoteStatus.Pending)
            return Result.Failure(Error.Conflict("Only a pending quote can be marked ready"));
        if (_items.Count == 0)
            return Result.Failure(Error.Validation("A quote needs at least one item"));

        Status = QuoteStatus.Ready;
        PromisedDeliveryDate = promisedDeliveryDate;
        TotalPrice = _items.Aggregate(
            Money.Zero(_items[0].UnitPrice.Currency),
            (sum, item) => sum.Add(item.UnitPrice.Multiply(item.Quantity)));

        return Result.Success();
    }

    public void MarkFailed(string reason)
    {
        Status = QuoteStatus.Failed;
        FailureReason = reason;
    }

    public Result MarkConverted(DateTime asOfUtc)
    {
        if (Status != QuoteStatus.Ready)
            return Result.Failure(Error.Conflict("Only a ready quote can be converted to an order"));
        if (IsExpired(asOfUtc))
        {
            Status = QuoteStatus.Expired;
            return Result.Failure(Error.Conflict("Quote has expired and must be regenerated"));
        }

        Status = QuoteStatus.Converted;
        return Result.Success();
    }
}
