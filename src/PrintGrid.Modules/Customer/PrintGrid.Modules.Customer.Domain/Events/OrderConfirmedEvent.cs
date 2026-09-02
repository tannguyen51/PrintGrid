using PrintGrid.SharedKernel.Common;

namespace PrintGrid.Modules.Customer.Domain.Events;

public sealed record OrderConfirmedEvent(
    Guid OrderId,
    Guid CustomerId,
    DateOnly PromisedDeliveryDate,
    IReadOnlyCollection<OrderConfirmedItem> Items) : DomainEvent;

public sealed record OrderConfirmedItem(
    Guid OrderItemId,
    Guid ModelId,
    int Quantity,
    string MaterialCode,
    string ColorCode,
    decimal LayerHeightMm,
    int InfillPercent,
    decimal ToleranceMm);
