using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrintGrid.Api.Authorization;
using PrintGrid.Modules.Customer.Application.Commands.Payments;
using PrintGrid.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Analytics.Domain.Entities;

namespace PrintGrid.Api.Controllers;

/// <summary>
/// FR-CUST-012 / BR-PAY-007/008 — deposit and settlement. Placement no longer confirms an order;
/// ops records payments here and the order is confirmed only once the deposit is covered.
/// The customer reads their own payment snapshot; ops/admin record payments.
/// </summary>
[ApiController]
[Route("api/v1/orders")]
public class OrderPaymentsController(ISender sender, PrintGridDbContext db) : ControllerBase
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
        return result.IsFailure ? ToError(result.Error) : Ok(result.Value);
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
                .Select(x => new PrintGrid.Modules.Customer.Application.DTOs.OrderPaymentDto(
                    x.PaidAmount >= x.DepositAmount, x.PaidAmount >= x.TotalPrice.Amount, x.PaidAmount))
                .SingleOrDefaultAsync(cancellationToken);
            return snapshot is null ? NotFound() : Ok(snapshot);
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
