using MediatR;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Commands.Shipment;

/// <summary>
/// Hub confirms physical handover of the parcel (Shipping → Delivered). This is the physical drop,
/// NOT the warranty trigger — the 30-day clock starts only when the customer confirms receipt (rule 2).
/// Invoked by hub staff (policy RequireHub) via <c>POST /hub/orders/{orderId}/deliver</c>.
/// </summary>
public sealed record DeliverOrderCommand(Guid OrderId) : IRequest<Result>;
