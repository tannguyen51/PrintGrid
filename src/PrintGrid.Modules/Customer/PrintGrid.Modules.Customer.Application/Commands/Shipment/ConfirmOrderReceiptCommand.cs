using MediatR;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Commands.Shipment;

/// <summary>
/// Customer confirms they received the goods. This is what stamps the 30-day warranty anchor
/// (<see cref="Domain.Entities.Order.DeliveredAt"/>) and records who confirmed. Requires the order
/// to already be Delivered by the hub. Customer-only, own-order, via <c>POST /orders/{orderId}/confirm-receipt</c>.
/// </summary>
public sealed record ConfirmOrderReceiptCommand(Guid OrderId, Guid CustomerId) : IRequest<Result>;
