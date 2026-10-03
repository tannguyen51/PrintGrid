using Microsoft.Extensions.Logging;
using PrintGrid.SharedKernel.Interfaces;

namespace PrintGrid.Infrastructure.Shared.Outbox;

/// <summary>
/// Durable email queue (outbox pattern). Status: Pending → Sent (dev channel = log).
/// Swap the delivery channel for SMTP/MailKit in T7 without touching producers.
/// </summary>
public class EmailOutboxItem
{
    public Guid Id { get; set; }
    public string ToEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? SentAtUtc { get; set; }
}

public class EmailOutboxStore : IEmailOutbox
{
    private readonly Persistence.PrintGridDbContext _context;
    private readonly ILogger<EmailOutboxStore> _logger;

    public EmailOutboxStore(Persistence.PrintGridDbContext context, ILogger<EmailOutboxStore> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task QueueAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
    {
        _context.EmailOutbox.Add(new EmailOutboxItem
        {
            Id = Guid.NewGuid(),
            ToEmail = toEmail,
            Subject = subject,
            Body = body,
            Status = "Pending",
            CreatedAtUtc = DateTime.UtcNow
        });
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> DeliverPendingAsync(CancellationToken cancellationToken = default)
    {
        var pending = _context.EmailOutbox
            .Where(e => e.Status == "Pending")
            .OrderBy(e => e.CreatedAtUtc)
            .Take(50)
            .ToList();

        foreach (var message in pending)
        {
            // DEV CHANNEL: log delivery. Replace with SMTP send in T7 (NFR: keep body free of secrets).
            _logger.LogInformation(
                "EMAIL → {To} | {Subject} | {BodyLength} chars",
                message.ToEmail, message.Subject, message.Body.Length);
            message.Status = "Sent";
            message.SentAtUtc = DateTime.UtcNow;
        }

        if (pending.Count > 0) await _context.SaveChangesAsync(cancellationToken);
        return pending.Count;
    }
}
