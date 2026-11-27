using MediatR;
using Microsoft.EntityFrameworkCore;
using PrintGrid.Api.Payments;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Infrastructure.Shared.Persistence.Notifications;
using PrintGrid.Modules.Customer.Application.Commands.Shipment;
using PrintGrid.Modules.Customer.Domain.Entities;
using PrintGrid.Modules.Customer.Domain.Enums;
using PrintGrid.SharedKernel.Events;

namespace PrintGrid.Api.BackgroundServices;

public sealed class CodConfirmationDeadlineProcessor(
    IServiceScopeFactory scopes, ILogger<CodConfirmationDeadlineProcessor> logger)
{
    public async Task RunAsync(DateTime? nowUtc = null, CancellationToken cancellationToken = default)
    {
        var now = nowUtc ?? DateTime.UtcNow;
        List<Guid> candidates;
        await using (var scope = scopes.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PrintGridDbContext>();
            var reminderBefore = now.AddHours(2);
            candidates = await db.Set<Order>().AsNoTracking()
                .Where(o => o.Status == OrderStatus.QualityCheck && o.PaymentMethod.ToUpper() == "COD"
                    && !o.IsCodConfirmed && (o.CodConfirmationDeadlineUtc == null || o.CodConfirmationDeadlineUtc <= reminderBefore))
                .OrderBy(o => o.CodConfirmationDeadlineUtc ?? o.CreatedAt).ThenBy(o => o.Id)
                .Select(o => o.Id).ToListAsync(cancellationToken);
        }

        foreach (var id in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try { await ProcessOrderAsync(id, nowUtc, cancellationToken); }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception ex) { logger.LogError(ex, "Failed to process COD deadline for order {OrderId}.", id); }
        }
    }

    private async Task ProcessOrderAsync(Guid id, DateTime? nowUtc, CancellationToken ct)
    {
        // A fresh scope avoids stale tracked orders. PostgreSQL row locks also serialize multiple replicas
        // against the customer's confirm/reject request, which uses the same transaction helper.
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PrintGridDbContext>();
        await using var transaction = await CodOrderTransaction.BeginAsync(db, id, ct);
        var order = await db.Set<Order>().SingleOrDefaultAsync(o => o.Id == id, ct);
        if (order is null || order.Status != OrderStatus.QualityCheck || order.IsCodConfirmed
            || !string.Equals(order.PaymentMethod, "COD", StringComparison.OrdinalIgnoreCase)) return;

        var now = nowUtc ?? DateTime.UtcNow;
        if (!order.CodConfirmationDeadlineUtc.HasValue)
        {
            // Recover older ready orders that never received a confirmation request. Give them
            // a full response window instead of treating the deployment as a timeout.
            var result = await scope.ServiceProvider.GetRequiredService<ISender>()
                .Send(new RequestCodConfirmationCommand(id), ct);
            if (result.IsFailure) { logger.LogWarning("Cannot request COD confirmation for {OrderId}: {Error}", id, result.Error.Message); return; }
        }
        else if (order.IsCodConfirmationExpired(now))
        {
            var cancelled = order.CancelDueToCodTimeout(now);
            if (cancelled.IsFailure) return;
            await db.SaveChangesAsync(ct);
            await scope.ServiceProvider.GetRequiredService<IPublisher>()
                .Publish(new OrderTimelineUpdatedIntegrationEvent(order.Id, order.CustomerId, "CodExpired"), ct);
        }
        else if (order.CodConfirmationDeadlineUtc.Value <= now.AddHours(2))
        {
            var requestedAt = order.CodConfirmationRequestedAtUtc ?? order.CodConfirmationDeadlineUtc.Value.AddHours(-24);
            var reminded = await db.UserNotifications.AnyAsync(n => n.UserId == order.CustomerId
                && n.Title.Contains(order.OrderNumber) && n.Title.Contains("sắp hết hạn") && n.CreatedAtUtc >= requestedAt, ct);
            if (!reminded)
            {
                var minutes = Math.Max(1, (int)Math.Ceiling((order.CodConfirmationDeadlineUtc.Value - now).TotalMinutes));
                db.UserNotifications.Add(UserNotification.Create(order.CustomerId,
                    $"Đơn hàng {order.OrderNumber} sắp hết hạn xác nhận",
                    $"Còn {minutes} phút để xác nhận sẽ nhận hàng COD. Quá hạn, đơn sẽ tự động hủy theo chính sách tiền cọc.", "warning", "/orders"));
                await db.SaveChangesAsync(ct);
            }
        }
        if (transaction is not null) await transaction.CommitAsync(ct);
    }
}
