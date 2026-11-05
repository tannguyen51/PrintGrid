using PrintGrid.SharedKernel.Interfaces;

namespace PrintGrid.Api.BackgroundJobs;

/// <summary>
/// Recurring Hangfire worker: flushes the durable email outbox (BR-IP-002, NOTIFY-001).
/// Runs every minute so the customer receipt for an upload arrives well within the ≤1 min
/// acceptance target. Dev channel = EmailOutboxStore logs; swap to SMTP in T7.
/// </summary>
public class EmailDeliveryJob(IEmailOutbox outbox, ILogger<EmailDeliveryJob> logger)
{
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var sent = await outbox.DeliverPendingAsync(cancellationToken);
        if (sent > 0)
            logger.LogInformation("Email outbox delivered {Count} message(s)", sent);
    }
}
