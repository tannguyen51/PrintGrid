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
using PrintGrid.Modules.Customer.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;

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
    /// <summary>Ops payment register for every order, including ledger and Lab reconciliation state.</summary>
    [HttpGet("payment-overview")]
    [Authorize(Policy = Policies.RequireOps)]
    public async Task<IActionResult> GetPaymentOverview(
        [FromQuery] string? paymentStatus,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var ordersQuery = db.Set<Order>().AsNoTracking();
        var normalizedSearch = search?.Trim().ToLower();
        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            ordersQuery = ordersQuery.Where(o =>
                o.OrderNumber.ToLower().Contains(normalizedSearch) ||
                o.CustomerId.ToString().ToLower().Contains(normalizedSearch));
        }

        var orders = await ordersQuery.OrderByDescending(o => o.CreatedAt).ToListAsync(cancellationToken);
        var orderIds = orders.Select(o => o.Id).ToList();
        var customers = await db.Set<Customer>().AsNoTracking()
            .Where(c => orders.Select(o => o.CustomerId).Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, cancellationToken);
        var ledger = await db.Set<FinancialTransaction>().AsNoTracking()
            .Where(t => orderIds.Contains(t.OrderId) && t.Status == "Confirmed")
            .GroupBy(t => t.OrderId)
            .Select(g => new
            {
                OrderId = g.Key,
                Receipts = g.Where(t => t.Type == "Receipt").Sum(t => t.Amount),
                Refunds = g.Where(t => t.Type == "Refund").Sum(t => t.Amount),
                Count = g.Count()
            }).ToDictionaryAsync(x => x.OrderId, cancellationToken);
        var completedLabJobs = await (from job in db.Set<Job>().AsNoTracking()
                                      join item in db.Set<OrderItem>().AsNoTracking() on job.OrderItemId equals item.Id
                                      where orderIds.Contains(item.OrderId) && job.Status == JobStatus.Completed && job.LabId != null
                                      group job by item.OrderId into g
                                      select new { OrderId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.OrderId, x => x.Count, cancellationToken);
        var labPayableRows = await db.Set<LabPayable>().AsNoTracking()
            .Where(p => orderIds.Contains(p.OrderId)).ToListAsync(cancellationToken);
        var labPayables = labPayableRows.GroupBy(p => p.OrderId).ToDictionary(
            g => g.Key,
            g => new { Count = g.Count(), Statuses = g.Select(p => p.Status).Distinct().ToList() });

        var rows = orders.Select(o =>
        {
            var status = PaymentStatus(o.PaidAmount, o.TotalPrice.Amount);
            ledger.TryGetValue(o.Id, out var ledgerRow);
            completedLabJobs.TryGetValue(o.Id, out var completedJobs);
            labPayables.TryGetValue(o.Id, out var payableRow);
            customers.TryGetValue(o.CustomerId, out var customer);
            var ledgerAmount = ledgerRow?.Receipts ?? 0m;
            var ledgerMatches = ledgerAmount == o.PaidAmount;
            var labMatches = completedJobs == (payableRow?.Count ?? 0);
            return new
            {
                orderId = o.Id, o.OrderNumber, o.CustomerId,
                customerName = customer?.FullName, customerEmail = customer?.Email,
                orderStatus = o.Status.ToString(), paymentStatus = status,
                totalAmount = o.TotalPrice.Amount, o.DepositAmount, o.PaidAmount,
                remaining = Math.Max(0m, o.TotalPrice.Amount - o.PaidAmount), o.RefundedAmount,
                currency = o.TotalPrice.Currency, o.CreatedAt, o.PromisedDeliveryDate,
                transactionCount = ledgerRow?.Count ?? 0, ledgerReceived = ledgerAmount,
                labJobCount = completedJobs, labPayableCount = payableRow?.Count ?? 0,
                labPayableStatuses = payableRow?.Statuses ?? [],
                reconciliationStatus = ledgerMatches && labMatches ? "Synced" : "Mismatch",
                reconciliationIssues = new[]
                {
                    ledgerMatches ? null : $"Ledger ghi nhận {ledgerAmount:N0}, đơn hàng ghi nhận {o.PaidAmount:N0} {o.TotalPrice.Currency}",
                    labMatches ? null : $"Có {completedJobs} job Lab hoàn tất nhưng {(payableRow?.Count ?? 0)} khoản công nợ"
                }.Where(x => x is not null)
            };
        }).Where(x => string.IsNullOrWhiteSpace(paymentStatus) ||
                      paymentStatus.Equals("All", StringComparison.OrdinalIgnoreCase) ||
                      x.paymentStatus.Equals(paymentStatus, StringComparison.OrdinalIgnoreCase))
          .ToList();

        return Ok(rows);
    }

    [HttpGet("{orderId:guid}/payment-detail")]
    [Authorize(Policy = Policies.RequireOps)]
    public async Task<IActionResult> GetPaymentDetail(Guid orderId, CancellationToken cancellationToken)
    {
        var order = await db.Set<Order>().AsNoTracking().SingleOrDefaultAsync(o => o.Id == orderId, cancellationToken);
        if (order is null) return NotFound();
        var transactions = await db.Set<FinancialTransaction>().AsNoTracking()
            .Where(t => t.OrderId == orderId).OrderByDescending(t => t.ConfirmedAtUtc).ToListAsync(cancellationToken);
        var payables = await db.Set<LabPayable>().AsNoTracking()
            .Where(p => p.OrderId == orderId).OrderByDescending(p => p.EligibleAtUtc).ToListAsync(cancellationToken);
        return Ok(new
        {
            orderId, order.OrderNumber, paymentStatus = PaymentStatus(order.PaidAmount, order.TotalPrice.Amount),
            totalAmount = order.TotalPrice.Amount, order.DepositAmount, order.PaidAmount,
            remaining = Math.Max(0m, order.TotalPrice.Amount - order.PaidAmount), order.RefundedAmount,
            currency = order.TotalPrice.Currency, transactions, labPayables = payables.Select(p => new
            {
                p.Id, p.JobId, p.LabId, p.GrossAmount, p.DeductionAmount, p.NetAmount,
                p.Currency, p.Status, p.EligibleAtUtc, p.ApprovedAtUtc, p.PaidAtUtc, p.PaymentReference
            })
        });
    }

    private static string PaymentStatus(decimal paid, decimal total) =>
        paid <= 0m ? "Unpaid" : paid < total ? "Partial" : "Paid";

    /// <summary>
    /// Backfills missing receipt entries for legacy orders. It never reduces an order amount when
    /// the ledger is higher; that unsafe direction remains visible as a reconciliation mismatch.
    /// </summary>
    [HttpPost("payment-reconciliation/sync")]
    [Authorize(Policy = Policies.RequireOps)]
    public async Task<IActionResult> SyncPaymentReconciliation(CancellationToken cancellationToken)
    {
        var actor = User.GetCustomerId();
        if (actor is null) return Forbid();
        var paidOrders = await db.Set<Order>().AsNoTracking().Where(o => o.PaidAmount > 0m)
            .Select(o => new { o.Id, o.PaidAmount, o.TotalPrice.Currency }).ToListAsync(cancellationToken);
        var ids = paidOrders.Select(o => o.Id).ToList();
        var receipts = await db.Set<FinancialTransaction>().AsNoTracking()
            .Where(t => ids.Contains(t.OrderId) && t.Type == "Receipt" && t.Status == "Confirmed")
            .GroupBy(t => t.OrderId).Select(g => new { OrderId = g.Key, Amount = g.Sum(t => t.Amount) })
            .ToDictionaryAsync(x => x.OrderId, x => x.Amount, cancellationToken);
        var created = 0;
        foreach (var order in paidOrders)
        {
            receipts.TryGetValue(order.Id, out var ledgerAmount);
            var missing = order.PaidAmount - ledgerAmount;
            if (missing <= 0m) continue;
            var key = $"order-payment-backfill:{order.Id:N}:{order.PaidAmount:0.00}";
            if (await db.Set<FinancialTransaction>().AnyAsync(t => t.Type == "Receipt" && t.IdempotencyKey == key, cancellationToken))
                continue;
            db.Set<FinancialTransaction>().Add(FinancialTransaction.Create(order.Id, "Receipt", missing,
                order.Currency, $"BACKFILL-{order.Id:N}", key, actor.Value, DateTime.UtcNow,
                note: "Backfill từ số tiền đã thanh toán trên đơn hàng khi đồng bộ đối soát."));
            created++;
        }
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { created });
    }

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
