using MediatR;
using Microsoft.Extensions.Logging;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Infrastructure.Shared.Persistence.Notifications;
using PrintGrid.Modules.Customer.Application.Commands.Shipment;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Events;

namespace PrintGrid.Api.EventHandlers;

/// <summary>
/// Automatically requests COD confirmation when order enters QualityCheck status.
/// Creates notification for customer with 24h deadline.
/// Customer has 24h to confirm they will receive the order (BR-PAY-008 COD model).
/// </summary>
public class QualityCheckCodConfirmationHandler(
    ISender sender,
    IOrderRepository orders,
    PrintGridDbContext dbContext,
    ILogger<QualityCheckCodConfirmationHandler> logger)
    : INotificationHandler<OrderTimelineUpdatedIntegrationEvent>
{
    public async Task Handle(OrderTimelineUpdatedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        if (notification.Status != "QualityCheck")
            return;

        logger.LogInformation(
            "Order {OrderId} entered QualityCheck — requesting COD confirmation from customer {CustomerId}.",
            notification.OrderId, notification.CustomerId);

        // Request COD confirmation
        var result = await sender.Send(new RequestCodConfirmationCommand(notification.OrderId), cancellationToken);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to request COD confirmation for order {OrderId}: {Error}",
                notification.OrderId, result.Error.Message);
            return;
        }

        // Get order details for notification
        var order = await orders.GetByIdAsync(notification.OrderId, cancellationToken);
        if (order is null)
        {
            logger.LogWarning("Order {OrderId} not found when creating notification.", notification.OrderId);
            return;
        }

        // Create notification for customer
        var notificationContent = new
        {
            Title = $"Đơn hàng {order.OrderNumber} đã sẵn sàng giao",
            Message = $"Đơn hàng của bạn đã hoàn thành kiểm tra chất lượng. Vui lòng xác nhận bạn sẽ nhận hàng trong vòng 24 giờ để chúng tôi tiến hành giao hàng. Nếu không xác nhận, đơn hàng sẽ tự động hủy và tiền cọc sẽ không được hoàn lại.",
            Link = "/orders"
        };

        var userNotification = UserNotification.Create(
            notification.CustomerId,
            notificationContent.Title,
            notificationContent.Message,
            "warning",
            notificationContent.Link);

        dbContext.UserNotifications.Add(userNotification);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Created COD confirmation notification {NotificationId} for customer {CustomerId} regarding order {OrderId}.",
            userNotification.Id, notification.CustomerId, notification.OrderId);
    }
}
