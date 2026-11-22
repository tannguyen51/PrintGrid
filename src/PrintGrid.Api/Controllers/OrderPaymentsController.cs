using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrintGrid.Api.Authorization;
using PrintGrid.Modules.Customer.Application.Commands.Payments;
using PrintGrid.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Analytics.Domain.Entities;
using Microsoft.Extensions.Options;
using PrintGrid.Api.Payments;

namespace PrintGrid.Api.Controllers;

/// <summary>
/// FR-CUST-012 / BR-PAY-007/008 — deposit and settlement. Placement no longer confirms an order;
/// ops records payments here and the order is confirmed only once the deposit is covered.
/// The customer reads their own payment snapshot; ops/admin record payments.
/// </summary>
[ApiController]
[Route("api/v1/orders")]
public class OrderPaymentsController(ISender sender, PrintGridDbContext db, IOptions<SePayOptions> sePayOptions) : ControllerBase
{
    /// <summary>Ops queue: orders still waiting for their deposit (PaymentPending).</summary>
    [HttpGet("pending-payment")]
    [Authorize(Policy = Policies.RequireOps)]
    public async Task<IActionResult> GetPendingPayment(
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetOrdersPendingPaymentQuery(search), cancellationToken);
        return result.IsFailure ? ToError(result.Error) : Ok(result.Value);
    }

    [HttpGet("{orderId:guid}/payment")]
    [Authorize(Policy = Policies.RequireCustomer)]
    public async Task<IActionResult> GetPayment(Guid orderId, CancellationToken cancellationToken)
    {
        var customerId = User.GetCustomerId();
        if (customerId is null) return Forbid();

        var result = await sender.Send(new GetOrderPaymentQuery(orderId, customerId.Value), cancellationToken);
        if (result.IsFailure) return ToError(result.Error);
        var payment = result.Value;
        var options = sePayOptions.Value;
        var transferDescription = IsVietinBank(options.BankCode)
            ? $"SEVQR {payment.PaymentCode}"
            : payment.PaymentCode;
        var qrUrl = options.CanCreateQr && payment.AmountDue > 0m
            ? $"{options.QrBaseUrl.TrimEnd('/')}?acc={Uri.EscapeDataString(options.AccountNumber)}" +
              $"&bank={Uri.EscapeDataString(options.BankCode)}&amount={payment.AmountDue:0}" +
              $"&des={Uri.EscapeDataString(transferDescription)}"
            : null;
        return Ok(new
        {
            payment.IsDeposited, payment.IsSettled, payment.PaidAmount, payment.TotalAmount,
            payment.DepositAmount, payment.Remaining, payment.AmountDue, payment.Currency,
            payment.PaymentCode, TransferDescription = transferDescription, QrUrl = qrUrl,
            BankCode = options.CanCreateQr ? options.BankCode : null,
            AccountNumber = options.CanCreateQr ? options.AccountNumber : null,
            AccountName = options.CanCreateQr ? options.AccountName : null,
            PaymentEnabled = options.CanCreateQr
        });
    }

    [HttpPost("{orderId:guid}/mark-paid")]
    [Authorize(Policy = Policies.RequireOps)]
    public async Task<IActionResult> MarkPaid(
        Guid orderId,
        [FromBody] MarkOrderPaidRequest? request,
        CancellationToken cancellationToken)
    {
        var actor = User.GetCustomerId();
        if (actor is null) return Forbid();
        var reference = string.IsNullOrWhiteSpace(request?.PaymentReference)
            ? $"MANUAL-{Guid.NewGuid():N}" : request.PaymentReference.Trim();
        var key = string.IsNullOrWhiteSpace(request?.IdempotencyKey) ? reference : request.IdempotencyKey.Trim();

        var existing = await db.Set<FinancialTransaction>().AsNoTracking()
            .SingleOrDefaultAsync(x => x.Type == "Receipt" && x.IdempotencyKey == key, cancellationToken);
        if (existing is not null)
        {
            var snapshot = await db.Set<PrintGrid.Modules.Customer.Domain.Entities.Order>().AsNoTracking()
                .Where(x => x.Id == orderId)
                .Select(x => new { x.Id, x.PaidAmount, x.DepositAmount, TotalAmount = x.TotalPrice.Amount, x.TotalPrice.Currency })
                .SingleOrDefaultAsync(cancellationToken);
            if (snapshot is null) return NotFound();
            var remaining = Math.Max(0m, snapshot.TotalAmount - snapshot.PaidAmount);
            return Ok(new PrintGrid.Modules.Customer.Application.DTOs.OrderPaymentDto(
                snapshot.PaidAmount >= snapshot.DepositAmount, snapshot.PaidAmount >= snapshot.TotalAmount,
                snapshot.PaidAmount, snapshot.TotalAmount, snapshot.DepositAmount, remaining,
                snapshot.PaidAmount >= snapshot.DepositAmount ? remaining : Math.Max(0m, snapshot.DepositAmount - snapshot.PaidAmount),
                snapshot.Currency, SePayPaymentCode.ForOrder(snapshot.Id)));
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var before = await db.Set<PrintGrid.Modules.Customer.Domain.Entities.Order>().AsNoTracking()
            .Where(x => x.Id == orderId).Select(x => x.PaidAmount).SingleOrDefaultAsync(cancellationToken);
        var result = await sender.Send(new MarkOrderPaidCommand(orderId, request?.Amount), cancellationToken);
        if (result.IsFailure) return ToError(result.Error);
        var applied = result.Value.PaidAmount - before;
        db.Set<FinancialTransaction>().Add(FinancialTransaction.Create(orderId, "Receipt", applied, "VND",
            reference, key, actor.Value, DateTime.UtcNow));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(result.Value);
    }

    private static bool IsVietinBank(string bankCode) =>
        bankCode.Equals("ICB", StringComparison.OrdinalIgnoreCase)
        || bankCode.Equals("970415", StringComparison.OrdinalIgnoreCase)
        || bankCode.Equals("VietinBank", StringComparison.OrdinalIgnoreCase);

    private IActionResult ToError(Error error)
    {
        var status = error.Code switch
        {
            "not_found" => StatusCodes.Status404NotFound,
            "conflict" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest,
        };
        return StatusCode(status, new { error = new { code = error.Code, message = error.Message } });
    }
}

/// <summary>Omit <paramref name="Amount"/> to settle the full remaining balance.</summary>
public sealed record MarkOrderPaidRequest(decimal? Amount = null, string? PaymentReference = null, string? IdempotencyKey = null);
