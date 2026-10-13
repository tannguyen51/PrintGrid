using MediatR;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Commands.Shipment;

/// <summary>A row in the hub shipment console (policy RequireHub) — orders the hub can act on.</summary>
public record ShipmentOrderDto(
    Guid Id,
    string OrderNumber,
    string Status,
    string? TrackingNumber,
    DateOnly PromisedDeliveryDate,
    DateTime? DeliveredAt,
    bool ReceiptConfirmed,
    Guid CustomerId = default,
    string DeliveryAddressSummary = "",
    DateTime? ReadyForPackingAtUtc = null,
    Guid? ConsolidatedShipmentId = null,
    string? SuggestedConsolidationGroupKey = null,
    IReadOnlyList<string>? SuggestedCompanionOrderNumbers = null,
    decimal TotalAmount = 0m,
    decimal RefundedAmount = 0m,
    int ItemCount = 0);

/// <summary>
/// Hub shipment queue: orders that are ready to ship (QualityCheck) or already in transit / handed over
/// (Shipping, Delivered), ordered by promised delivery date. Powers <c>GET /hub/orders/shipment-queue</c>.
/// </summary>
public sealed record GetShipmentQueueQuery : IRequest<Result<IReadOnlyList<ShipmentOrderDto>>>;
