using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Infrastructure.Shared.Persistence.Notifications;
using PrintGrid.Modules.Customer.Domain.Entities;
using PrintGrid.Modules.Customer.Domain.Enums;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;

namespace PrintGrid.Api.BackgroundServices;

/// <summary>
/// Background service to monitor COD confirmation deadlines.
/// - Sends reminder notifications 2 hours before deadline
/// - Auto-cancels orders when deadline passes
/// Runs every 15 minutes.
/// </summary>
public class CodConfirmationDeadlineMonitorService(
    IServiceProvider serviceProvider,
    ILogger<CodConfirmationDeadlineMonitorService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("COD Confirmation Deadline Monitor Service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckDeadlinesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while checking COD confirmation deadlines.");
            }

            // Wait 15 minutes before next check
            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
        }
    }

    private async Task CheckDeadlinesAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PrintGridDbContext>();
        var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var now = DateTime.UtcNow;
        var twoHoursFromNow = now.AddHours(2);

        // Find orders with COD confirmation deadline within 2 hours (for reminder)
        var ordersNeedingReminder = await dbContext.Set<Order>()
            .Where(o => o.Status == OrderStatus.QualityCheck
                && string.Equals(o.PaymentMethod, "COD")
                && !o.IsCodConfirmed
                && o.CodConfirmationDeadlineUtc.HasValue
                && o.CodConfirmationDeadlineUtc > now
                && o.CodConfirmationDeadlineUtc <= twoHoursFromNow)
            .ToListAsync(cancellationToken);

        foreach (var order in ordersNeedingReminder)
        {
            await SendReminderNotificationAsync(dbContext, order, cancellationToken);
        }

        // Find orders with expired COD confirmation deadline (for auto-cancel)
        var expiredOrders = await dbContext.Set<Order>()
            .Where(o => o.Status == OrderStatus.QualityCheck
                && string.Equals(o.PaymentMethod, "COD")
                && !o.IsCodConfirmed
                && o.CodConfirmationDeadlineUtc.HasValue
                && o.CodConfirmationDeadlineUtc <= now)
            .ToListAsync(cancellationToken);

        foreach (var order in expiredOrders)
        {
            await AutoCancelOrderAsync(orderRepository, unitOfWork, dbContext, order, cancellationToken);
        }
    }

    private async Task SendReminderNotificationAsync(
        PrintGridDbContext dbContext,
        Order order,
        CancellationToken cancellationToken)
    {
        // Check if reminder already sent (avoid duplicates)
        var reminderAlreadySent = await dbContext.UserNotifications
            .AnyAsync(n => n.UserId == order.CustomerId
                && n.Title.Contains(order.OrderNumber)
                && n.Title.Contains("sắp hết hạn")
                && n.CreatedAtUtc > order.CodConfirmationRequestedAtUtc,
                cancellationToken);

        if (reminderAlreadySent)
            return;

        var timeRemaining = order.CodConfirmationDeadlineUtc!.Value - DateTime.UtcNow;
        var hoursRemaining = (int)timeRemaining.TotalHours;
        var minutesRemaining = (int)timeRemaining.TotalMinutes % 60;

        var notification = UserNotification.Create(
            order.CustomerId,
            $"Đơn hàng {order.OrderNumber} sắp hết hạn xác nhận",
            $"Đơn hàng của bạn sẽ tự động hủy trong {hoursRemaining} giờ {minutesRemaining} phút nếu không xác nhận nhận hàng. Vui lòng xác nhận ngay để tránh mất tiền cọc.",
            "error",
            "/orders");

        dbContext.UserNotifications.Add(notification);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Sent COD confirmation reminder notification for order {OrderNumber} to customer {CustomerId}. Time remaining: {Hours}h {Minutes}m.",
            order.OrderNumber, order.CustomerId, hoursRemaining, minutesRemaining);
    }

    private async Task AutoCancelOrderAsync(
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        PrintGridDbContext dbContext,
        Order order,
        CancellationToken cancellationToken)
    {
        try
        {
            // Cancel the order
            var cancelResult = order.CancelDueToCodTimeout();
            if (cancelResult.IsFailure)
            {
                logger.LogWarning(
                    "Failed to auto-cancel order {OrderNumber}: {Error}",
                    order.OrderNumber, cancelResult.Error.Message);
                return;
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);

            // Create cancellation notification
            var notification = UserNotification.Create(
                order.CustomerId,
                $"Đơn hàng {order.OrderNumber} đã bị hủy",
                $"Đơn hàng của bạn đã bị hủy do quá thời gian xác nhận nhận hàng (24 giờ). Theo chính sách BR-PAY-010, tiền cọc {order.DepositAmount:N0} VND sẽ không được hoàn lại. Vui lòng liên hệ hỗ trợ nếu bạn vẫn muốn đặt hàng.",
                "error",
                "/orders");

            dbContext.UserNotifications.Add(notification);
            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogWarning(
                "Auto-cancelled order {OrderNumber} due to COD confirmation timeout. Customer {CustomerId} lost deposit of {DepositAmount:N0} VND.",
                order.OrderNumber, order.CustomerId, order.DepositAmount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while auto-cancelling order {OrderNumber}.", order.OrderNumber);
        }
    }
}
