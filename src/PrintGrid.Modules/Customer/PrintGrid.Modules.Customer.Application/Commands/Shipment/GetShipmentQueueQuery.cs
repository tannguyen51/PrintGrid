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
    bool ReceiptConfirmed);

/// <summary>
/// Hub shipment queue: orders that are ready to ship (QualityCheck) or already in transit / handed over
/// (Shipping, Delivered), ordered by promised delivery date. Powers <c>GET /hub/orders/shipment-queue</c>.
/// </summary>
public sealed record GetShipmentQueueQuery : IRequest<Result<IReadOnlyList<ShipmentOrderDto>>>;
