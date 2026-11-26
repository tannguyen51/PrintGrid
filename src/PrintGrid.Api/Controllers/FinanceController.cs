using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrintGrid.Api.Authorization;
using PrintGrid.Api.Finance;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Analytics.Domain.Entities;
using PrintGrid.Modules.Customer.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;

namespace PrintGrid.Api.Controllers;

[ApiController]
[Route("api/v1/finance")]
[Authorize]
public sealed class FinanceController(PrintGridDbContext db) : ControllerBase
{
    [HttpGet("transactions")]
    [Authorize(Policy = Policies.RequireOps)]
    public async Task<IActionResult> Transactions([FromQuery] Guid? orderId, CancellationToken ct)
    {
        var query = db.Set<FinancialTransaction>().AsNoTracking();
        if (orderId.HasValue) query = query.Where(x => x.OrderId == orderId);
        return Ok(await query.OrderByDescending(x => x.ConfirmedAtUtc).Take(1000).ToListAsync(ct));
    }

    [HttpPost("lab-payables/sync")]
    [Authorize(Policy = Policies.RequireOps)]
    public async Task<IActionResult> SyncPayables(CancellationToken ct)
    {
        var rows = await (from job in db.Set<Job>().AsNoTracking()
                          join item in db.Set<OrderItem>().AsNoTracking() on job.OrderItemId equals item.Id
                          where job.Status == JobStatus.Completed && job.LabId != null
                          select new { Job = job, Item = item }).ToListAsync(ct);
        var calculator = await JobEarningsCalculator.LoadAsync(db, rows.Select(r => r.Item.Id).Distinct().ToList(), ct);
        var jobIds = rows.Select(r => r.Job.Id).ToList();
        var existing = await db.Set<LabPayable>().Where(p => jobIds.Contains(p.JobId)).ToDictionaryAsync(p => p.JobId, ct);
        var created = 0;
        var corrected = 0;
        foreach (var row in rows)
        {
            var earnings = calculator.Calculate(row.Job);
            if (existing.TryGetValue(row.Job.Id, out var payable))
            {
                if (await ReconcilePayableAsync(payable, earnings, ct)) corrected++;
                continue;
            }
            // The producer receives the payable; another lab's fault must not remove this producer's income.
            db.Set<LabPayable>().Add(LabPayable.Create(row.Job.Id, row.Item.OrderId,
                row.Job.LabId!.Value, earnings.GrossAmount, earnings.DeductionAmount,
                row.Item.UnitPrice.Currency, row.Job.CompletedAtUtc ?? DateTime.UtcNow));
            created++;
        }
        await db.SaveChangesAsync(ct);
        return Ok(new { created, corrected });
    }

    [HttpGet("lab-payables")]
    public async Task<IActionResult> Payables([FromQuery] string? status, [FromQuery] Guid? labId, CancellationToken ct)
    {
        var query = db.Set<LabPayable>().AsNoTracking();
        if (!User.IsInRole(Roles.OpsManager) && !User.IsInRole(Roles.Admin))
        {
            var actor = User.GetCustomerId();
            if (actor is null) return Forbid();
            var allowed = await db.Set<Lab>().Where(x => x.OwnerUserId == actor).Select(x => x.Id).ToListAsync(ct);
            query = query.Where(x => allowed.Contains(x.LabId));
        }
        else if (labId.HasValue) query = query.Where(x => x.LabId == labId);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status);
        return Ok(await query.OrderByDescending(x => x.EligibleAtUtc).ToListAsync(ct));
    }

    [HttpPost("lab-payables/{id:guid}/approve")]
    [Authorize(Policy = Policies.RequireOps)]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct) =>
        await ChangePayable(id, null, ct);

    [HttpPost("lab-payables/{id:guid}/pay")]
    [Authorize(Policy = Policies.RequireOps)]
    public async Task<IActionResult> Pay(Guid id, PayLabRequest request, CancellationToken ct) =>
        await ChangePayable(id, request.Reference, ct);

    [HttpGet("reports")]
    [Authorize(Policy = Policies.RequireOps)]
    public async Task<IActionResult> Reports([FromQuery] DateTime? fromUtc, [FromQuery] DateTime? toUtc, CancellationToken ct)
    {
        var from = fromUtc ?? DateTime.UnixEpoch; var to = toUtc ?? DateTime.MaxValue;
        var tx = await db.Set<FinancialTransaction>().AsNoTracking()
            .Where(x => x.ConfirmedAtUtc >= from && x.ConfirmedAtUtc <= to).ToListAsync(ct);
        var payables = await db.Set<LabPayable>().AsNoTracking()
            .Where(x => x.EligibleAtUtc >= from && x.EligibleAtUtc <= to).ToListAsync(ct);
        var receipts = tx.Where(x => x.Type == "Receipt").Sum(x => x.Amount);
        var refunds = tx.Where(x => x.Type == "Refund").Sum(x => x.Amount);
        var paidToLabs = payables.Where(x => x.Status == "Paid").Sum(x => x.NetAmount);
        return Ok(new
        {
            customerReceipts = new { totalReceived = receipts, totalRefunded = refunds, netReceived = receipts - refunds },
            labPayments = new { totalPayable = payables.Sum(x => x.NetAmount), totalPaid = paidToLabs, outstanding = payables.Where(x => x.Status != "Paid").Sum(x => x.NetAmount) },
            business = new { revenueAfterRefunds = receipts - refunds, labCostsRecognized = payables.Sum(x => x.NetAmount), remainingCash = receipts - refunds - paidToLabs },
            transactions = tx.OrderByDescending(x => x.ConfirmedAtUtc)
        });
    }

    private async Task<IActionResult> ChangePayable(Guid id, string? reference, CancellationToken ct)
    {
        var actor = User.GetCustomerId(); if (actor is null) return Forbid();
        var payable = await db.Set<LabPayable>().SingleOrDefaultAsync(x => x.Id == id, ct);
        if (payable is null) return NotFound();
        if (payable.Status != "Paid" || payable.NetAmount == 0m)
        {
            var job = await db.Set<Job>().AsNoTracking().SingleOrDefaultAsync(j => j.Id == payable.JobId, ct);
            if (job is null) return Conflict(new { error = new { code = "conflict", message = "Không tìm thấy job của khoản công nợ." } });
            var calculator = await JobEarningsCalculator.LoadAsync(db, [job.OrderItemId], ct);
            if (await ReconcilePayableAsync(payable, calculator.Calculate(job), ct))
            {
                await db.SaveChangesAsync(ct);
                return Conflict(new { error = new { code = "conflict", message = "Tiền công in lại đã được cập nhật. Vui lòng kiểm tra số tiền mới và duyệt lại trước khi chi." } });
            }
        }
        var result = reference is null ? payable.Approve(actor.Value, DateTime.UtcNow) : payable.MarkPaid(actor.Value, reference, DateTime.UtcNow);
        if (result.IsFailure) return Conflict(new { error = new { result.Error.Code, result.Error.Message } });
        await db.SaveChangesAsync(ct); return Ok(payable);
    }

    private async Task<bool> ReconcilePayableAsync(LabPayable payable, JobEarnings earnings, CancellationToken ct)
    {
        if (!payable.NeedsDeductionReconciliation(earnings.DeductionAmount)) return false;
        if (payable.Status == "Paid")
        {
            // This is a correction of a zero-value marker, never a cash receipt or a new payment.
            // Archive the old approval/payment metadata before reopening the outstanding fee.
            var key = $"lab-zero-correction:{payable.Id:N}";
            if (!await db.Set<FinancialTransaction>().AnyAsync(t => t.Type == "LabCorrection" && t.IdempotencyKey == key, ct))
            {
                var note = System.Text.Json.JsonSerializer.Serialize(new
                {
                    payableId = payable.Id, previousPaidBy = payable.PaidBy, previousPaidAtUtc = payable.PaidAtUtc,
                    previousApprovedBy = payable.ApprovedBy, previousApprovedAtUtc = payable.ApprovedAtUtc,
                    previousDeduction = payable.DeductionAmount, correctedDeduction = earnings.DeductionAmount
                });
                db.Set<FinancialTransaction>().Add(FinancialTransaction.Create(payable.OrderId, "LabCorrection", 0m,
                    payable.Currency, payable.PaymentReference ?? $"ZERO-{payable.Id:N}", key,
                    ControllerContext.HttpContext?.User.GetCustomerId() ?? payable.PaidBy ?? Guid.Empty, DateTime.UtcNow, note: note));
            }
        }
        return payable.ReconcileUnpaidDeduction(earnings.DeductionAmount);
    }
}

public sealed record PayLabRequest(string Reference);
