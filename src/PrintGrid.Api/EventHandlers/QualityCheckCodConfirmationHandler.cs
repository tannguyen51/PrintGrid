using MediatR;
using PrintGrid.Modules.Customer.Application.Commands.Shipment;
using PrintGrid.Modules.Customer.Domain.Enums;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Events;

namespace PrintGrid.Api.EventHandlers;

/// <summary>Request once when the aggregate is ready, without resetting the persisted deadline on event replay.</summary>
public sealed class QualityCheckCodConfirmationHandler(ISender sender, IOrderRepository orders,
    ILogger<QualityCheckCodConfirmationHandler> logger) : INotificationHandler<OrderTimelineUpdatedIntegrationEvent>
{
    public async Task Handle(OrderTimelineUpdatedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        if (notification.Status is not ("QualityCheck" or "Completed")) return;
        var order = await orders.GetByIdAsync(notification.OrderId, cancellationToken);
        if (order is null || order.Status != OrderStatus.QualityCheck || order.IsCodConfirmed
            || order.CodConfirmationDeadlineUtc.HasValue || !string.Equals(order.PaymentMethod, "COD", StringComparison.OrdinalIgnoreCase)) return;
        var result = await sender.Send(new RequestCodConfirmationCommand(order.Id), cancellationToken);
        if (result.IsFailure) logger.LogWarning("Failed to request COD confirmation for order {OrderId}: {Error}", order.Id, result.Error.Message);
    }
}
