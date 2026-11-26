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
[Route("api/v1/lab/finance")]
[Authorize(Roles = Roles.LabManager)]
public sealed class LabFinanceController(PrintGridDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Report([FromQuery] DateOnly? from = null, [FromQuery] DateOnly? to = null,
        [FromQuery] string scope = "completed", [FromQuery] string? paymentStatus = null,
        [FromQuery] string? search = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (from > to || from == DateOnly.MinValue || to == DateOnly.MaxValue || page is < 1 or > 1_000_000
            || pageSize is < 1 or > 100 || scope is not ("completed" or "all")
            || paymentStatus is not (null or "" or "all" or "PendingSync" or "Eligible" or "Approved" or "Paid"))
            return BadRequest(new { error = new { code = "validation", message = "Bộ lọc hoặc phân trang không hợp lệ." } });

        var actor = User.GetCustomerId();
        if (actor is null) return Forbid();
        // Financial data belongs to the owner. Never use the unclaimed-lab fallback used by legacy job boards.
        var lab = await db.Set<Lab>().AsNoTracking().SingleOrDefaultAsync(x => x.OwnerUserId == actor.Value, ct);
        if (lab is null) return NotFound(new { error = new { code = "not_found", message = "Bạn chưa có hồ sơ Lab." } });

        var jobs = db.Set<Job>().AsNoTracking().Where(j => j.LabId == lab.Id
            && j.Status != JobStatus.Cancelled && j.Status != JobStatus.Failed);
        if (scope == "completed") jobs = jobs.Where(j => j.Status == JobStatus.Completed);
        // Completed report is a cohort by Vietnam inspection/completion date. The price book includes all assigned jobs.
        if (from.HasValue)
        {
            var start = DateTime.SpecifyKind(from.Value.ToDateTime(TimeOnly.MinValue).AddHours(-7), DateTimeKind.Utc);
            jobs = jobs.Where(j => (j.InspectedAtUtc ?? j.CompletedAtUtc ?? j.CreatedAt) >= start);
        }
        if (to.HasValue)
        {
            var end = DateTime.SpecifyKind(to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue).AddHours(-7), DateTimeKind.Utc);
            jobs = jobs.Where(j => (j.InspectedAtUtc ?? j.CompletedAtUtc ?? j.CreatedAt) < end);
        }

        var rows = await (from job in jobs
                          join item in db.Set<OrderItem>().AsNoTracking() on job.OrderItemId equals item.Id
                          join order in db.Set<Order>().AsNoTracking() on item.OrderId equals order.Id
                          join payable in db.Set<LabPayable>().AsNoTracking().Where(p => p.LabId == lab.Id)
                              on job.Id equals payable.JobId into paymentRows
                          from payable in paymentRows.DefaultIfEmpty()
                          select new
                          {
                              Job = job, order.OrderNumber, OrderId = order.Id,
                              LineQuantity = item.Quantity,
                              LineTotal = item.BatchTotalAmount ?? item.UnitPrice.Amount * item.Quantity,
                              Currency = item.UnitPrice.Currency, Payable = payable
                          }).ToListAsync(ct);

        var calculator = await JobEarningsCalculator.LoadAsync(db, rows.Select(r => r.Job.OrderItemId).Distinct().ToList(), ct);
        var prices = rows.Select(row =>
        {
            var split = LabRevenueShare.Calculate(row.LineTotal, row.LineQuantity, row.Job.Quantity);
            var ownFault = row.Job.CostBearer == CostBearer.Lab
                && (row.Job.FaultLabId ?? row.Job.LabId) == lab.Id;
            var earnings = calculator.Calculate(row.Job);
            var needsCorrection = row.Payable is not null && row.Payable.DeductionAmount != earnings.DeductionAmount;
            return new LabFinanceJobDto(row.Job.Id, row.OrderId, row.OrderNumber,
                row.Job.Specification.MaterialCode, row.Job.Specification.Technology.ToString(),
                row.Job.ReprintIndex > 0 ? (ownFault ? "LabReprint" : "PaidReprint") : "Standard",
                row.Job.Quantity, row.Job.Status.ToString(), row.Job.InspectedAtUtc ?? row.Job.CompletedAtUtc,
                row.Job.CreatedAt, split.JobValue, earnings.GrossAmount, split.HubAmount, earnings.DeductionAmount,
                earnings.NetAmount, row.Currency,
                needsCorrection ? "PendingSync" : row.Payable?.Status ?? (row.Job.Status == JobStatus.Completed ? "PendingSync" : "Estimated"),
                row.Payable?.PaidAtUtc, row.Payable?.PaymentReference);
        }).ToList();

        // Only Completed (hub-accepted) production earns income. Failed attempts and cancelled split parents earn nothing.
        var completed = prices.Where(x => x.JobStatus == nameof(JobStatus.Completed)).ToList();
        var total = completed.Sum(x => x.NetAmount);
        var paid = completed.Where(x => x.PaymentStatus == "Paid").Sum(x => x.NetAmount);
        var summary = new LabFinanceSummaryDto(completed.Count, completed.Sum(x => x.Quantity),
            completed.Count(x => x.JobType != "Standard"), completed.Sum(x => x.JobValue),
            completed.Sum(x => x.LabAmount), completed.Sum(x => x.HubAmount),
            completed.Sum(x => x.DeductionAmount), total, paid, total - paid,
            total > 0 ? decimal.Round(paid / total * 100m, 2) : 0m,
            completed.Count(x => x.PaymentStatus == "PendingSync"));

        var byType = completed.GroupBy(x => new { x.MaterialCode, x.Technology, x.JobType })
            .OrderBy(g => g.Key.MaterialCode).ThenBy(g => g.Key.Technology).ThenBy(g => g.Key.JobType)
            .Select(g => new LabFinanceTypeDto(g.Key.MaterialCode, g.Key.Technology, g.Key.JobType,
                g.Count(), g.Sum(x => x.Quantity), g.Sum(x => x.NetAmount),
                g.Where(x => x.PaymentStatus == "Paid").Sum(x => x.NetAmount))).ToList();

        IEnumerable<LabFinanceJobDto> filtered = prices;
        if (paymentStatus is not (null or "" or "all")) filtered = filtered.Where(x => x.PaymentStatus == paymentStatus);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            filtered = filtered.Where(x => x.OrderNumber.Contains(term, StringComparison.OrdinalIgnoreCase)
                || x.JobId.ToString().Contains(term, StringComparison.OrdinalIgnoreCase)
                || x.MaterialCode.Contains(term, StringComparison.OrdinalIgnoreCase));
        }
        var list = filtered.OrderByDescending(x => x.CompletedAtUtc ?? x.CreatedAtUtc).ThenBy(x => x.JobId).ToList();
        return Ok(new LabFinanceReportDto(lab.Id, lab.Name, LabRevenueShare.LabRate, LabRevenueShare.HubRate,
            summary, byType, list.Skip((page - 1) * pageSize).Take(pageSize).ToList(), list.Count));
    }
}

public sealed record LabFinanceReportDto(Guid LabId, string LabName, decimal LabRate, decimal HubRate,
    LabFinanceSummaryDto Summary, IReadOnlyList<LabFinanceTypeDto> ByType,
    IReadOnlyList<LabFinanceJobDto> Items, int TotalCount);
public sealed record LabFinanceSummaryDto(int CompletedJobs, int CompletedProducts, int ReprintJobs,
    decimal ProductionValue, decimal LabGrossAmount, decimal HubAmount, decimal Deductions,
    decimal TotalIncome, decimal TotalReceived, decimal Outstanding, decimal CollectionRate, int PendingSyncJobs);
public sealed record LabFinanceTypeDto(string MaterialCode, string Technology, string JobType,
    int JobCount, int Quantity, decimal Income, decimal Received);
public sealed record LabFinanceJobDto(Guid JobId, Guid OrderId, string OrderNumber, string MaterialCode,
    string Technology, string JobType, int Quantity, string JobStatus, DateTime? CompletedAtUtc,
    DateTime CreatedAtUtc, decimal JobValue, decimal LabAmount, decimal HubAmount, decimal DeductionAmount,
    decimal NetAmount, string Currency, string PaymentStatus, DateTime? PaidAtUtc, string? PaymentReference);
