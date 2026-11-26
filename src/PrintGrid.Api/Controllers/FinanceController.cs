using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrintGrid.Api.Authorization;
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
        var known = await db.Set<LabPayable>().Select(x => x.JobId).ToListAsync(ct);
        var rows = await (from job in db.Set<Job>().AsNoTracking()
                          join item in db.Set<OrderItem>().AsNoTracking() on job.OrderItemId equals item.Id
                          where job.Status == JobStatus.Completed && job.LabId != null && !known.Contains(job.Id)
                          select new { Job = job, Item = item }).ToListAsync(ct);
        foreach (var row in rows)
        {
            var gross = decimal.Round(row.Item.LineTotal.Amount * row.Job.Quantity / row.Item.Quantity * 0.70m, 2);
            var deduction = row.Job.CostBearer == CostBearer.Lab ? gross : 0m;
            db.Set<LabPayable>().Add(LabPayable.Create(row.Job.Id, row.Item.OrderId,
                row.Job.FaultLabId ?? row.Job.LabId!.Value, gross, deduction,
                row.Item.UnitPrice.Currency, row.Job.CompletedAtUtc ?? DateTime.UtcNow));
        }
        await db.SaveChangesAsync(ct);
        return Ok(new { created = rows.Count });
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

    [HttpGet("lab-summary")]
    [Authorize(Policy = Policies.RequireOps)]
    public async Task<IActionResult> LabSummary([FromQuery] Guid? labId, [FromQuery] DateTime? fromUtc, [FromQuery] DateTime? toUtc, CancellationToken ct)
    {
        var from = fromUtc ?? DateTime.UnixEpoch; var to = toUtc ?? DateTime.MaxValue;
        var query = db.Set<LabPayable>().AsNoTracking()
            .Where(x => x.EligibleAtUtc >= from && x.EligibleAtUtc <= to);
        if (labId.HasValue) query = query.Where(x => x.LabId == labId.Value);
        var payables = await query.ToListAsync(ct);
        var labIds = payables.Select(x => x.LabId).Distinct();
        var labs = await db.Set<Lab>().AsNoTracking().Where(x => labIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.Name, ct);
        var summary = payables.GroupBy(x => x.LabId).Select(g => new
        {
            labId = g.Key,
            labName = labs.GetValueOrDefault(g.Key) ?? "Unknown",
            totalPayable = g.Sum(x => x.NetAmount),
            paid = g.Where(x => x.Status == "Paid").Sum(x => x.NetAmount),
            outstanding = g.Where(x => x.Status != "Paid").Sum(x => x.NetAmount),
            transactionCount = g.Count()
        }).OrderByDescending(x => x.totalPayable);
        return Ok(summary);
    }

    [HttpGet("revenue-by-period")]
    [Authorize(Policy = Policies.RequireOps)]
    public async Task<IActionResult> RevenueByPeriod([FromQuery] Guid? labId, [FromQuery] DateTime? fromUtc, [FromQuery] DateTime? toUtc, [FromQuery] string period = "week", CancellationToken ct = default)
    {
        var from = fromUtc ?? DateTime.UtcNow.AddMonths(-3); var to = toUtc ?? DateTime.UtcNow;
        var txQuery = db.Set<FinancialTransaction>().AsNoTracking()
            .Where(x => x.ConfirmedAtUtc >= from && x.ConfirmedAtUtc <= to && x.Type == "Receipt");
        var receipts = await txQuery.ToListAsync(ct);
        var grouped = period.ToLower() switch
        {
            "day" => receipts.GroupBy(x => x.ConfirmedAtUtc.Date),
            "month" => receipts.GroupBy(x => new DateTime(x.ConfirmedAtUtc.Year, x.ConfirmedAtUtc.Month, 1)),
            _ => receipts.GroupBy(x => System.Globalization.CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(x.ConfirmedAtUtc, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday) > 0
                ? x.ConfirmedAtUtc.Date.AddDays(-(int)x.ConfirmedAtUtc.DayOfWeek + 1) : x.ConfirmedAtUtc.Date)
        };
        var data = grouped.Select(g => new { period = g.Key.ToString("yyyy-MM-dd"), total = g.Sum(x => x.Amount), count = g.Count() }).OrderBy(x => x.period);
        return Ok(data);
    }

    private async Task<IActionResult> ChangePayable(Guid id, string? reference, CancellationToken ct)
    {
        var actor = User.GetCustomerId(); if (actor is null) return Forbid();
        var payable = await db.Set<LabPayable>().SingleOrDefaultAsync(x => x.Id == id, ct);
        if (payable is null) return NotFound();
        var result = reference is null ? payable.Approve(actor.Value, DateTime.UtcNow) : payable.MarkPaid(actor.Value, reference, DateTime.UtcNow);
        if (result.IsFailure) return Conflict(new { error = new { result.Error.Code, result.Error.Message } });
        await db.SaveChangesAsync(ct); return Ok(payable);
    }
}

public sealed record PayLabRequest(string Reference);
