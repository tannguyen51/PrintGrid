namespace PrintGrid.SharedKernel.Interfaces;

/// <summary>
/// Durable outbox for outbound emails (BR-IP-002 and friends). Handlers queue here
/// inside the business transaction; a background worker delivers (dev: logs; prod: SMTP).
/// </summary>
public interface IEmailOutbox
{
    Task QueueAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default);

    /// <summary>Delivers all pending messages; returns how many were sent.</summary>
    Task<int> DeliverPendingAsync(CancellationToken cancellationToken = default);
}
