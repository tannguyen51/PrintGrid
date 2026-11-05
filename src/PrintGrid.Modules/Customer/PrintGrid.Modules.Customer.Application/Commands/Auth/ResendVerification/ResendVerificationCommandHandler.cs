using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using PrintGrid.Modules.Customer.Application.Auth;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;
using System.Text.Json;

namespace PrintGrid.Modules.Customer.Application.Commands.Auth.ResendVerification;

public class ResendVerificationCommandHandler(
    ICustomerRepository customers,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    IEmailService emailService,
    IConfiguration configuration,
    IDistributedCache cache)
    : IRequestHandler<ResendVerificationCommand, Result<ResendVerificationResponse>>
{
    public async Task<Result<ResendVerificationResponse>> Handle(ResendVerificationCommand command, CancellationToken cancellationToken)
    {
        var cacheKey = $"verification_resend_{command.Email.ToLowerInvariant()}";
        var cacheData = await cache.GetStringAsync(cacheKey, cancellationToken);
        var history = string.IsNullOrEmpty(cacheData)
            ? []
            : JsonSerializer.Deserialize<List<DateTime>>(cacheData)!;

        // Cleanup old entries (> 1 hour)
        var oneHourAgo = DateTime.UtcNow.AddHours(-1);
        history.RemoveAll(t => t < oneHourAgo);

        switch (history.Count)
        {
            // Check max 5 per hour
            case >= 5:
                return Result.Failure<ResendVerificationResponse>(new Error("rate_limit_exceeded", "Too many attempts. Please try again later."));
            // Check 60s cooldown
            case > 0 when (DateTime.UtcNow - history.Last()).TotalSeconds < 60:
            {
                var waitTime = 60 - (int)(DateTime.UtcNow - history.Last()).TotalSeconds;
                return Result.Failure<ResendVerificationResponse>(new Error("cooldown_active", $"Please wait {waitTime} seconds before resending."));
            }
        }

        // We do not leak if email exists. We just process it silently if not found or already verified.
        var customer = await customers.GetByEmailAsync(command.Email, cancellationToken);
        if (customer != null && !customer.IsEmailVerified)
        {
            var rawToken = Guid.NewGuid().ToString("N");
            var tokenHash = passwordHasher.Hash(rawToken);
            customer.SetVerificationToken(tokenHash, TimeSpan.FromHours(24));

            await unitOfWork.SaveChangesAsync(cancellationToken);

            var baseUrl = configuration["App:FrontendUrl"] ?? "http://localhost:5173";
            var verifyLink = $"{baseUrl}/verify-email?token={rawToken}&email={Uri.EscapeDataString(customer.Email)}";
            var emailBody = $"<p>Hi {customer.FullName},</p><p>Please verify your email by clicking <a href=\"{verifyLink}\">here</a>.</p>";
            await emailService.SendEmailAsync(customer.Email, "Verify your PrintGrid account", emailBody, cancellationToken);
        }

        history.Add(DateTime.UtcNow);
        await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(history), new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) }, cancellationToken);

        return Result.Success(new ResendVerificationResponse());
    }
}
