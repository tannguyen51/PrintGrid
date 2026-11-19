using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrintGrid.Api.Authorization;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Customer.Domain.Entities;
using PrintGrid.Modules.Customer.Domain.Enums;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;

namespace PrintGrid.Api.Controllers;

[ApiController]
[Route("api/v1/ops/orders")]
[Authorize(Roles = $"{Roles.Admin},{Roles.OpsManager}")]
public sealed class OpsOrdersController(PrintGridDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] DateOnly? from, [FromQuery] DateOnly? to,
        [FromQuery] string? category, [FromQuery] string? search, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        if (from > to || to == DateOnly.MaxValue || from == DateOnly.MinValue || page is < 1 or > 1_000_000 || pageSize is < 1 or > 100)
            return BadRequest(new { error = new { code = "validation", message = "Khoảng ngày hoặc phân trang không hợp lệ." } });
        if (category is not (null or "" or "all" or "delivered" or "cancelled" or "incident" or "open-incident" or "refunded" or "partial-refund" or "full-refund"))
            return BadRequest(new { error = new { code = "validation", message = "Bộ lọc không hợp lệ." } });
        var orders = db.Set<Order>().AsNoTracking();
        // Report dates are Vietnam business dates; the interval includes the whole final day.
        if (from.HasValue)
        {
            var start = DateTime.SpecifyKind(from.Value.ToDateTime(TimeOnly.MinValue).AddHours(-7), DateTimeKind.Utc);
            orders = orders.Where(o => o.CreatedAt >= start);
        }
        if (to.HasValue)
        {
            var end = DateTime.SpecifyKind(to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue).AddHours(-7), DateTimeKind.Utc);
            orders = orders.Where(o => o.CreatedAt < end);
        }
        var jobs = db.Set<Job>();
        // Correlated EXISTS yields exactly one row per order, regardless of split or reprint count.
        var rows = orders.Select(o => new
        {
            o.Id, o.OrderNumber, o.Status, o.CreatedAt, o.RequestedDeliveryDate, o.PromisedDeliveryDate, o.DispatchMode,
            Quantity = o.Items.Sum(i => i.Quantity), TotalAmount = o.TotalPrice.Amount, o.PaidAmount, o.RefundedAmount,
            HasIncident = o.IsDelayed || o.Items.Any(i => jobs.Any(j => j.OrderItemId == i.Id &&
                (j.Status == JobStatus.Failed || j.ReprintIndex > 0 || j.AttemptNumber > 1 || j.QcProofStatus == QcProofStatus.Rejected))),
            HasOpenIncident = o.Status != OrderStatus.Delivered && o.Status != OrderStatus.Cancelled && o.Items.Any(i =>
                jobs.Any(j => j.OrderItemId == i.Id && j.Status == JobStatus.Failed && !jobs.Any(r =>
                    r.OrderItemId == i.Id && r.ReprintIndex > j.ReprintIndex && r.Status != JobStatus.Cancelled &&
                    (r.OriginalJobId ?? r.Id) == (j.OriginalJobId ?? j.Id))))
        });
        var summary = await rows.GroupBy(_ => 1).Select(g => new
        {
            TotalOrders = g.Count(), DeliveredOrders = g.Count(o => o.Status == OrderStatus.Delivered),
            CancelledOrders = g.Count(o => o.Status == OrderStatus.Cancelled), IncidentOrders = g.Count(o => o.HasIncident),
            OpenIncidentOrders = g.Count(o => o.HasOpenIncident), RefundedOrders = g.Count(o => o.RefundedAmount > 0),
            PartialRefundOrders = g.Count(o => o.RefundedAmount > 0 && o.RefundedAmount < o.PaidAmount),
            FullyRefundedOrders = g.Count(o => o.PaidAmount > 0 && o.RefundedAmount >= o.PaidAmount)
        }).SingleOrDefaultAsync(ct);
        var filtered = category switch
        {
            "delivered" => rows.Where(o => o.Status == OrderStatus.Delivered),
            "cancelled" => rows.Where(o => o.Status == OrderStatus.Cancelled),
            "incident" => rows.Where(o => o.HasIncident),
            "open-incident" => rows.Where(o => o.HasOpenIncident),
            "refunded" => rows.Where(o => o.RefundedAmount > 0),
            "partial-refund" => rows.Where(o => o.RefundedAmount > 0 && o.RefundedAmount < o.PaidAmount),
            "full-refund" => rows.Where(o => o.PaidAmount > 0 && o.RefundedAmount >= o.PaidAmount),
            _ => rows
        };
        if (!string.IsNullOrWhiteSpace(search)) filtered = filtered.Where(o => o.OrderNumber.Contains(search.Trim()));
        var totalCount = await filtered.CountAsync(ct);
        var items = await filtered.OrderByDescending(o => o.CreatedAt).ThenBy(o => o.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Ok(new
        {
            summary = (object?)summary ?? new { TotalOrders = 0, DeliveredOrders = 0, CancelledOrders = 0, IncidentOrders = 0,
                OpenIncidentOrders = 0, RefundedOrders = 0, PartialRefundOrders = 0, FullyRefundedOrders = 0 },
            items = items.Select(o => new { o.Id, o.OrderNumber, status = o.Status.ToString(), o.CreatedAt,
                o.RequestedDeliveryDate, o.PromisedDeliveryDate, o.DispatchMode, o.Quantity, o.TotalAmount, o.PaidAmount,
                o.RefundedAmount, o.HasIncident, o.HasOpenIncident,
                refundKind = o.RefundedAmount <= 0 ? "None" : o.RefundedAmount >= o.PaidAmount ? "Full" : "Partial" }),
            totalCount, page, pageSize
        });
    }

    [HttpGet("{orderId:guid}")]
    public async Task<IActionResult> Detail(Guid orderId, CancellationToken ct)
    {
        var order = await db.Set<Order>().AsNoTracking().Include(o => o.Items).SingleOrDefaultAsync(o => o.Id == orderId, ct);
        if (order is null) return NotFound();
        var ids = order.Items.Select(i => i.Id).ToList();
        var batches = await db.Set<Job>().AsNoTracking().Where(j => ids.Contains(j.OrderItemId))
            .OrderBy(j => j.CreatedAt).Select(j => new { j.Id, j.OrderItemId, j.ParentJobId, j.OriginalJobId,
                j.ReprintIndex, j.Quantity, j.Status, j.LabId, j.MachineId, j.PlannedStartUtc, j.PlannedEndUtc, j.FailureReason }).ToListAsync(ct);
        var labIds = batches.Where(j => j.LabId.HasValue).Select(j => j.LabId!.Value).Distinct().ToList();
        var names = await db.Set<Lab>().Where(l => labIds.Contains(l.Id)).ToDictionaryAsync(l => l.Id, l => l.Name, ct);
        return Ok(new { order.Id, order.OrderNumber, status = order.Status.ToString(), order.RequestedDeliveryDate,
            order.PromisedDeliveryDate, order.DispatchMode, order.PaidAmount, order.RefundedAmount,
            items = order.Items.Select(i => new { i.Id, i.Quantity, i.ItemStatus,
                completedQuantity = batches.Where(j => j.OrderItemId == i.Id && j.Status == JobStatus.Completed).Sum(j => j.Quantity) }),
            batches = batches.Select(j => new { j.Id, j.OrderItemId, j.ParentJobId, j.OriginalJobId, j.ReprintIndex,
                j.Quantity, status = j.Status.ToString(), j.LabId, labName = j.LabId.HasValue ? names.GetValueOrDefault(j.LabId.Value) : null,
                j.MachineId, j.PlannedStartUtc, j.PlannedEndUtc, j.FailureReason }) });
    }
}
