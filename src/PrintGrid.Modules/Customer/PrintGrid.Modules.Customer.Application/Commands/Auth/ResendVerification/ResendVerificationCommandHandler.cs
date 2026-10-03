using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using PrintGrid.Modules.Customer.Application.Auth;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;
using System.Text.Json;

namespace PrintGrid.Modules.Customer.Application.Commands.Auth.ResendVerification;

public class ResendVerificationCommandHandler : IRequestHandler<ResendVerificationCommand, Result<ResendVerificationResponse>>
{
    private readonly ICustomerRepository _customers;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly IDistributedCache _cache;

    public ResendVerificationCommandHandler(
        ICustomerRepository customers,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IEmailService emailService,
        IConfiguration configuration,
        IDistributedCache cache)
    {
        _customers = customers;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
        _configuration = configuration;
        _cache = cache;
    }

    public async Task<Result<ResendVerificationResponse>> Handle(ResendVerificationCommand command, CancellationToken cancellationToken)
    {
        var cacheKey = $"verification_resend_{command.Email.ToLowerInvariant()}";
        var cacheData = await _cache.GetStringAsync(cacheKey, cancellationToken);
        var history = string.IsNullOrEmpty(cacheData) 
            ? new List<DateTime>() 
            : JsonSerializer.Deserialize<List<DateTime>>(cacheData)!;

        // Cleanup old entries (> 1 hour)
        var oneHourAgo = DateTime.UtcNow.AddHours(-1);
        history.RemoveAll(t => t < oneHourAgo);

        // Check max 5 per hour
        if (history.Count >= 5)
        {
            return Result.Failure<ResendVerificationResponse>(new Error("rate_limit_exceeded", "Too many attempts. Please try again later."));
        }

        // Check 60s cooldown
        if (history.Count > 0 && (DateTime.UtcNow - history.Last()).TotalSeconds < 60)
        {
            var waitTime = 60 - (int)(DateTime.UtcNow - history.Last()).TotalSeconds;
            return Result.Failure<ResendVerificationResponse>(new Error("cooldown_active", $"Please wait {waitTime} seconds before resending."));
        }

        // We do not leak if email exists. We just process it silently if not found or already verified.
        var customer = await _customers.GetByEmailAsync(command.Email, cancellationToken);
        if (customer != null && !customer.IsEmailVerified)
        {
            var rawToken = Guid.NewGuid().ToString("N");
            var tokenHash = _passwordHasher.Hash(rawToken);
            customer.SetVerificationToken(tokenHash, TimeSpan.FromHours(24));
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var baseUrl = _configuration["App:FrontendUrl"] ?? "http://localhost:5173";
            var verifyLink = $"{baseUrl}/verify-email?token={rawToken}&email={Uri.EscapeDataString(customer.Email)}";
            var emailBody = $"<p>Hi {customer.FullName},</p><p>Please verify your email by clicking <a href=\"{verifyLink}\">here</a>.</p>";
            await _emailService.SendEmailAsync(customer.Email, "Verify your PrintGrid account", emailBody, cancellationToken);
        }

        history.Add(DateTime.UtcNow);
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(history), new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) }, cancellationToken);

        return Result.Success(new ResendVerificationResponse(60));
    }
}
