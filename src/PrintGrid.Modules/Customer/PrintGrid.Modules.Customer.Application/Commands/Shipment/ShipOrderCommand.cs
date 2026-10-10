using MediatR;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Commands.Shipment;

/// <summary>
/// Hub goods-out: attach a carrier tracking number and move the order into transit (QualityCheck → Shipping).
/// Invoked by hub staff (policy RequireHub) via <c>POST /hub/orders/{orderId}/ship</c>.
/// </summary>
public sealed record ShipOrderCommand(Guid OrderId, string TrackingNumber) : IRequest<Result>;
