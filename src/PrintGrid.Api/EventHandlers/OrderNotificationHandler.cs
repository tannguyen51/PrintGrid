using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PrintGrid.Api.Authorization;
using PrintGrid.Api.Hubs;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Infrastructure.Shared.Persistence.Notifications;
using PrintGrid.Modules.Customer.Domain.Entities;
using PrintGrid.SharedKernel.Events;

namespace PrintGrid.Api.EventHandlers;

public sealed class OrderNotificationHandler : INotificationHandler<OrderTimelineUpdatedIntegrationEvent>
{
    private readonly PrintGridDbContext _dbContext;
    private readonly IHubContext<NotificationHub> _hubContext;

    public OrderNotificationHandler(
        PrintGridDbContext dbContext,
        IHubContext<NotificationHub> hubContext)
    {
        _dbContext = dbContext;
        _hubContext = hubContext;
    }

    public async Task Handle(OrderTimelineUpdatedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var (customerTitle, customerMessage, staffNotifications) = GetNotificationContent(notification);

        var recipients = new HashSet<Guid> { notification.CustomerId };
        var newNotifications = new List<UserNotification>
        {
            UserNotification.Create(
                notification.CustomerId,
                customerTitle,
                customerMessage,
                "order",
                notification.Status?.StartsWith("Cod", StringComparison.Ordinal) == true ? "/orders" : $"/orders/{notification.OrderId}"),
        };

        foreach (var staffNotification in staffNotifications)
        {
            var roleRecipients = await _dbContext.Set<Customer>()
                .AsNoTracking()
                .Where(customer => customer.Roles.Contains(staffNotification.Role))
                .Select(customer => customer.Id)
                .ToListAsync(cancellationToken);

            foreach (var recipientId in roleRecipients)
            {
                if (recipients.Add(recipientId))
                {
                    newNotifications.Add(UserNotification.Create(
                        recipientId,
                        staffNotification.Title,
                        staffNotification.Message,
                        "work-queue",
                        staffNotification.Link));
                }
            }
        }

        await _dbContext.UserNotifications.AddRangeAsync(newNotifications, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        foreach (var recipientId in newNotifications.Select(n => n.UserId).Distinct())
        {
            await _hubContext.Clients.Group(NotificationHub.UserGroup(recipientId))
                .SendAsync("NotificationsUpdated", cancellationToken);
        }
    }

    private static (
        string CustomerTitle,
        string CustomerMessage,
        RoleNotification[] StaffNotifications) GetNotificationContent(OrderTimelineUpdatedIntegrationEvent notification)
    {
        return notification.Status switch
        {
            "CodConfirmationRequested" => (
                "Vui lòng xác nhận nhận hàng COD",
                "Đơn đã sẵn sàng tại Hub. Bạn có 24 giờ để xác nhận sẽ nhận hàng và thanh toán phần còn lại cho shipper. Quá hạn, đơn sẽ hủy theo chính sách tiền cọc.", []),
            "CodConfirmed" => ("Đã xác nhận sẽ nhận hàng", "Hub sẽ tiến hành giao hàng. Bạn thanh toán phần tiền còn lại cho shipper khi nhận hàng.",
                [new(Roles.HubFulfillment, "Khách đã xác nhận COD", "Đơn đã được khách xác nhận sẽ nhận hàng.", "/hub/shipments")]),
            "CodRejected" => ("Đơn hàng đã hủy", "Đơn đã được hủy theo yêu cầu từ chối nhận hàng của bạn. Tiền cọc được xử lý theo chính sách đã chấp thuận.",
                [new(Roles.HubFulfillment, "Khách đã từ chối COD", "Đơn đã hủy; không tiếp tục giao hàng.", "/hub/shipments")]),
            "CodExpired" => ("Đơn hàng đã hủy do quá hạn COD", "Đơn đã tự động hủy vì không xác nhận nhận hàng trong 24 giờ. Tiền cọc được xử lý theo chính sách đã chấp thuận.",
                [new(Roles.HubFulfillment, "Đơn COD quá hạn đã hủy", "Không tiếp tục giao đơn chưa được khách xác nhận đúng hạn.", "/hub/shipments")]),
            "Printing" => (
                "Sản xuất đã bắt đầu",
                "Đơn hàng của bạn đã bắt đầu được in.",
                [
                    new(Roles.LabManager, "Cập nhật hàng đợi sản xuất", "Một đơn hàng đã bắt đầu được sản xuất.", "/lab/queue"),
                    new(Roles.LabOperator, "Cập nhật hàng đợi sản xuất", "Một đơn hàng đã bắt đầu được sản xuất.", "/lab/queue"),
                ]),
            "QualityCheck" => (
                "Đơn hàng đang được kiểm tra",
                "Sản phẩm đã chuyển sang bước kiểm tra chất lượng.",
                [
                    new(Roles.HubQc, "Có sản phẩm chờ kiểm tra", "Một sản phẩm đã chuyển sang bước kiểm tra chất lượng.", "/hub/qc"),
                ]),
            "Completed" => (
                "Đã hoàn tất kiểm tra",
                "Sản phẩm của bạn đã hoàn tất kiểm tra chất lượng.",
                [
                    new(Roles.HubFulfillment, "Cập nhật xử lý giao hàng", "Một sản phẩm đã hoàn tất kiểm tra chất lượng.", "/hub/shipments"),
                ]),
            "Failed" or "Delayed" => (
                "Đơn hàng cần được chú ý",
                "Đơn hàng của bạn gặp sự cố trong quá trình xử lý. Vui lòng xem chi tiết đơn hàng.",
                [
                    new(Roles.OpsManager, "Có sự cố cần điều phối", "Một đơn hàng gặp sự cố cần được xem xét.", "/ops/escalations"),
                    new(Roles.Admin, "Sự cố trong hệ thống", "Một đơn hàng gặp sự cố trong quá trình sản xuất.", "/admin"),
                ]),
            _ => (
                "Đơn hàng được cập nhật",
                "Trạng thái xử lý hoặc giao hàng của đơn hàng đã được cập nhật.",
                []),
        };
    }

    private sealed record RoleNotification(string Role, string Title, string Message, string Link);
}
