using PrintGrid.SharedKernel.Common;

namespace PrintGrid.Modules.Customer.Domain.Events;

public sealed record OrderConfirmedEvent(
    Guid OrderId = default,
    IReadOnlyList<OrderConfirmedItem>? Items = null,
    DateOnly PromisedDeliveryDate = default) : DomainEvent;

public sealed record OrderConfirmedItem(
    Guid OrderItemId,
    Guid ModelId,
    int Quantity,
    string MaterialCode,
    string ColorCode,
    decimal LayerHeightMm,
    int EstimatedPrintMinutes,
    decimal EstimatedMaterialGrams,
    decimal? BoundingWidthMm,
    decimal? BoundingDepthMm,
    decimal? BoundingHeightMm,
    string? CalculationJson = null,
    Guid? QuotedMachineId = null,
    decimal ToleranceMm = 0.2m);
