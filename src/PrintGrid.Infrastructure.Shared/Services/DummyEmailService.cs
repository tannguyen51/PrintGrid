using Microsoft.Extensions.Logging;
using PrintGrid.SharedKernel.Interfaces;

namespace PrintGrid.Infrastructure.Shared.Services;

public class DummyEmailService(ILogger<DummyEmailService> logger) : IEmailService
{
    public Task SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("--- SENDING EMAIL ---");
        logger.LogInformation("To: {ToEmail}", toEmail);
        logger.LogInformation("Subject: {Subject}", subject);
        logger.LogInformation("Body: {HtmlBody}", htmlBody);
        logger.LogInformation("---------------------");
        return Task.CompletedTask;
    }
}
