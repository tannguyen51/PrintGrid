using MediatR;
using PrintGrid.Modules.Customer.Domain.Enums;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Events;
using PrintGrid.SharedKernel.Interfaces;

namespace PrintGrid.Modules.Customer.Application.EventHandlers;

public class ProductionStatusChangedHandler :
    INotificationHandler<ProductionStatusChangedIntegrationEvent>,
    INotificationHandler<OrderDelayedIntegrationEvent>
{
    private readonly IOrderRepository _orders;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;

    public ProductionStatusChangedHandler(IOrderRepository orders, IUnitOfWork unitOfWork, IPublisher publisher)
    {
        _orders = orders;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
    }

    public async Task Handle(ProductionStatusChangedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var order = await _orders.GetByItemIdAsync(notification.OrderItemId, cancellationToken);
        if (order == null) return;

        var item = order.Items.FirstOrDefault(i => i.Id == notification.OrderItemId);
        if (item != null)
        {
            item.UpdateStatus(notification.NewStatus);
            if (notification.NewStatus == "Failed")
            {
                order.MarkAsDelayed();
                // We should also notify the customer (BR-NOTIFY-001)
            }
            else if (notification.NewStatus == "Printing")
            {
                order.TransitionTo(OrderStatus.InProduction);
            }
            else if (notification.NewStatus == "QualityCheck")
            {
                // Check if all items are completed or in quality check
                if (order.Items.All(i => i.ItemStatus is "QualityCheck" or "Completed"))
                {
                    order.TransitionTo(OrderStatus.QualityCheck);
                }
            }
            else if (notification.NewStatus == "Completed")
            {
                // "Completed" = job finished + hub inspection passed + proof approved. It is an ITEM-level
                // signal only: the order must NOT jump forward here. When the order is already QualityCheck
                // it stays QualityCheck until the hub ships it, which is gated on every item being Completed.
            }
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _publisher.Publish(new OrderTimelineUpdatedIntegrationEvent(order.Id, order.CustomerId), cancellationToken);
        }
    }

    public async Task Handle(OrderDelayedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var order = await _orders.GetByItemIdAsync(notification.OrderItemId, cancellationToken);
        if (order == null) return;

        order.MarkAsDelayed();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _publisher.Publish(new OrderTimelineUpdatedIntegrationEvent(order.Id, order.CustomerId), cancellationToken);
    }
}
