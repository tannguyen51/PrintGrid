using PrintGrid.Modules.Customer.Domain.Enums;
using PrintGrid.Modules.Customer.Domain.Events;
using PrintGrid.SharedKernel.Common;
using PrintGrid.SharedKernel.Results;
using PrintGrid.SharedKernel.ValueObjects;

namespace PrintGrid.Modules.Customer.Domain.Entities;

public class Order : AggregateRoot<Guid>
{
    private readonly List<OrderItem> _items = new();

    public Guid CustomerId { get; private set; }
    public Guid QuoteId { get; private set; }
    public string OrderNumber { get; private set; } = string.Empty;
    public OrderStatus Status { get; private set; }
    public Money TotalPrice { get; private set; } = Money.Zero();
    public DateOnly PromisedDeliveryDate { get; private set; }
    public Address DeliveryAddress { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }
    public string? PaymentTransactionId { get; private set; }
    public bool IsDelayed { get; private set; }

    /// <summary>
    /// Warranty anchor — the moment the CUSTOMER confirms receipt (30-day guarantee starts here).
    /// The hub's physical "delivered" handover deliberately does NOT stamp this (rule 2).
    /// </summary>
    public DateTime? DeliveredAt { get; private set; }

    /// <summary>Carrier tracking number captured by the hub when it ships the order (FR-HUB / BR: required).</summary>
    public string? TrackingNumber { get; private set; }

    /// <summary>The customer who confirmed receipt — pairs with <see cref="DeliveredAt"/> (who/when).</summary>
    public Guid? ReceiptConfirmedBy { get; private set; }

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    public void MarkAsDelayed() => IsDelayed = true;

    private Order() { }

    public static Order CreateFromQuote(Quote quote, Address deliveryAddress, string orderNumber)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = quote.CustomerId,
            QuoteId = quote.Id,
            OrderNumber = orderNumber,
            Status = OrderStatus.PaymentPending,
            TotalPrice = quote.TotalPrice,
            PromisedDeliveryDate = quote.PromisedDeliveryDate,
            DeliveryAddress = deliveryAddress,
            CreatedAt = DateTime.UtcNow,
            IsDelayed = false
        };

        foreach (var quoteItem in quote.Items)
        {
            order._items.Add(OrderItem.FromQuoteItem(order.Id, quoteItem));
        }

        return order;
    }

    public Result ConfirmPayment(string paymentTransactionId)
    {
        if (Status != OrderStatus.PaymentPending)
            return Result.Failure(Error.Conflict($"Order {OrderNumber} is already past payment"));

        Status = OrderStatus.Confirmed;
        PaymentTransactionId = paymentTransactionId;
        ConfirmedAt = DateTime.UtcNow;

        AddDomainEvent(new OrderConfirmedEvent(
            Id,
            CustomerId,
            PromisedDeliveryDate,
            _items.Select(i => new OrderConfirmedItem(
                i.Id,
                i.ModelId,
                i.Quantity,
                i.Configuration.MaterialCode,
                i.Configuration.ColorCode,
                i.Configuration.LayerHeightMm,
                i.Configuration.InfillPercent,
                i.Configuration.ToleranceMm,
                i.EstimatedPrintMinutes,
                i.EstimatedMaterialGrams,
                i.BoundingWidthMm,
                i.BoundingDepthMm,
                i.BoundingHeightMm)).ToList()));

        return Result.Success();
    }

    public Result TransitionTo(OrderStatus next)
    {
        var allowed = Status switch
        {
            OrderStatus.Confirmed => next is OrderStatus.InProduction or OrderStatus.Cancelled,
            OrderStatus.InProduction => next is OrderStatus.QualityCheck or OrderStatus.Cancelled,
            OrderStatus.QualityCheck => next is OrderStatus.Shipping or OrderStatus.InProduction,
            OrderStatus.Shipping => next is OrderStatus.Delivered,
            _ => false
        };

        if (!allowed)
            return Result.Failure(Error.Conflict($"Cannot move order from {Status} to {next}"));

        Status = next;
        return Result.Success();
    }

    /// <summary>
    /// Hub goods-out. Requires a tracking number (BR: bắt buộc có vận đơn) and can only run on a
    /// quality-checked order; moves it to <see cref="OrderStatus.Shipping"/> and stores the carrier code.
    /// </summary>
    public Result Ship(string? trackingNumber)
    {
        if (string.IsNullOrWhiteSpace(trackingNumber))
            return Result.Failure(Error.Validation("Tracking number is required to ship the order."));

        var moved = TransitionTo(OrderStatus.Shipping);
        if (moved.IsFailure)
            return moved;

        TrackingNumber = trackingNumber.Trim();
        return Result.Success();
    }

    /// <summary>
    /// Hub confirms physical handover of the parcel. Moves the order to <see cref="OrderStatus.Delivered"/>
    /// but deliberately does NOT touch <see cref="DeliveredAt"/> — the warranty clock starts only when the
    /// customer confirms receipt (rule 2).
    /// </summary>
    public Result MarkDelivered() => TransitionTo(OrderStatus.Delivered);

    /// <summary>
    /// Customer confirms they actually received the goods. This is the event that stamps the 30-day
    /// warranty anchor (<see cref="DeliveredAt"/>) and records who confirmed. Only valid after the hub
    /// has marked the order delivered.
    /// </summary>
    public Result ConfirmReceipt(Guid confirmedBy)
    {
        if (Status != OrderStatus.Delivered)
            return Result.Failure(Error.Conflict($"Only a delivered order can be confirmed by the customer (current status: {Status})."));

        DeliveredAt = DateTime.UtcNow;
        ReceiptConfirmedBy = confirmedBy;
        return Result.Success();
    }
}
