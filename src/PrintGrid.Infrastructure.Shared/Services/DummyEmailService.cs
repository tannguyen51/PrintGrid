using Microsoft.Extensions.Logging;
using PrintGrid.SharedKernel.Interfaces;

namespace PrintGrid.Infrastructure.Shared.Services;

public class DummyEmailService : IEmailService
{
    private readonly ILogger<DummyEmailService> _logger;

    public DummyEmailService(ILogger<DummyEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("--- SENDING EMAIL ---");
        _logger.LogInformation("To: {ToEmail}", toEmail);
        _logger.LogInformation("Subject: {Subject}", subject);
        _logger.LogInformation("Body: {HtmlBody}", htmlBody);
        _logger.LogInformation("---------------------");
        return Task.CompletedTask;
    }
}
