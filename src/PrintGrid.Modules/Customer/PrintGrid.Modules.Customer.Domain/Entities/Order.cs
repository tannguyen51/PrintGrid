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

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

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
            CreatedAt = DateTime.UtcNow
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
                i.Configuration.ToleranceMm)).ToList()));

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
}
