using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PrintGrid.Api.Payments;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Analytics.Domain.Entities;
using PrintGrid.Modules.Customer.Application.Commands.Payments;
using PrintGrid.Modules.Customer.Domain.Entities;

namespace PrintGrid.Api.Controllers;

[ApiController]
[Route("api/v1/webhooks/sepay")]
public sealed class SePayWebhookController(
    PrintGridDbContext db,
    IOptions<SePayOptions> options,
    ILogger<SePayWebhookController> logger) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [HttpPost, AllowAnonymous]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var rawBody = await reader.ReadToEndAsync(cancellationToken);
        var config = options.Value;
        if (!config.Enabled)
            return StatusCode(503, new { success = false, message = "SePay is disabled." });
        if (!Authenticate(rawBody, config))
            return Unauthorized(new { success = false, message = "Invalid SePay webhook credentials." });

        SePayWebhookPayload? payload;
        try { payload = JsonSerializer.Deserialize<SePayWebhookPayload>(rawBody, JsonOptions); }
        catch (JsonException) { return BadRequest(new { success = false, message = "Invalid JSON payload." }); }

        if (payload is null || payload.Id <= 0 || payload.TransferAmount <= 0m)
            return BadRequest(new { success = false, message = "Invalid transaction payload." });
        if (!string.Equals(payload.TransferType, "in", StringComparison.OrdinalIgnoreCase))
            return StatusCode(201, new { success = true });
        if (!string.IsNullOrWhiteSpace(config.AccountNumber)
            && !string.Equals(Normalize(payload.AccountNumber), Normalize(config.AccountNumber), StringComparison.Ordinal))
            return BadRequest(new { success = false, message = "Unexpected receiving account." });

        var paymentText = string.Join(' ', payload.Code, payload.Content, payload.Description);
        if (!SePayPaymentCode.TryGetOrderId(paymentText, out var orderId))
            return BadRequest(new { success = false, message = "Payment code was not found." });

        var idempotencyKey = $"SEPAY:{payload.Id}";
        if (await AlreadyHandled(idempotencyKey, cancellationToken))
            return StatusCode(201, new { success = true });

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        // Serialize payments for one order so two distinct bank transactions cannot lose an update.
        var order = await db.Set<Order>()
            .FromSqlInterpolated($"SELECT * FROM customer.orders WHERE \"Id\" = {orderId} FOR UPDATE")
            .Include(x => x.Items)
            .SingleOrDefaultAsync(cancellationToken);
        if (order is null) return NotFound(new { success = false, message = "Order was not found." });

        var applied = order.ApplyPayment(payload.TransferAmount, payload.ReferenceCode ?? payload.Id.ToString());
        if (applied.IsFailure) return Conflict(new { success = false, message = applied.Error.Message });

        db.Set<FinancialTransaction>().Add(FinancialTransaction.Create(
            order.Id, "Receipt", payload.TransferAmount, order.TotalPrice.Currency,
            payload.ReferenceCode ?? payload.Id.ToString(), idempotencyKey, Guid.Empty, DateTime.UtcNow,
            note: $"SePay/{payload.Gateway}"));
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            logger.LogWarning(exception, "Duplicate or concurrent SePay webhook {TransactionId}", payload.Id);
            await transaction.RollbackAsync(cancellationToken);
            db.ChangeTracker.Clear();
            if (await AlreadyHandled(idempotencyKey, cancellationToken))
                return StatusCode(201, new { success = true });
            throw;
        }
        return StatusCode(201, new { success = true });
    }

    private Task<bool> AlreadyHandled(string key, CancellationToken ct) =>
        db.Set<FinancialTransaction>().AsNoTracking().AnyAsync(x => x.Type == "Receipt" && x.IdempotencyKey == key, ct);

    private bool Authenticate(string body, SePayOptions config)
    {
        if (!string.IsNullOrWhiteSpace(config.WebhookSecret))
        {
            var supplied = Request.Headers["X-SePay-Signature"].ToString().Trim();
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(config.WebhookSecret));
            var digest = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
            return FixedEquals(supplied, Convert.ToHexString(digest))
                || FixedEquals(supplied, Convert.ToHexString(digest).ToLowerInvariant())
                || FixedEquals(supplied, Convert.ToBase64String(digest));
        }
        if (!string.IsNullOrWhiteSpace(config.WebhookApiKey))
            return FixedEquals(Request.Headers.Authorization.ToString(), $"Apikey {config.WebhookApiKey}");
        logger.LogError("SePay is enabled without webhook credentials.");
        return false;
    }

    private static bool FixedEquals(string left, string right)
    {
        var a = Encoding.UTF8.GetBytes(left);
        var b = Encoding.UTF8.GetBytes(right);
        return a.Length == b.Length && CryptographicOperations.FixedTimeEquals(a, b);
    }

    private static string Normalize(string? value) => new((value ?? string.Empty).Where(char.IsLetterOrDigit).ToArray());
}

public sealed record SePayWebhookPayload(
    long Id, string? Gateway, string? TransactionDate, string? AccountNumber,
    string? SubAccount, string? Code, string? Content, string? TransferType,
    string? Description, decimal TransferAmount, decimal Accumulated, string? ReferenceCode);
