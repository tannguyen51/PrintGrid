using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrintGrid.Api.Authorization;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Customer.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;

namespace PrintGrid.Api.Controllers;

[ApiController]
[Route("api/v1/orders")]
[Authorize(Policy = Policies.RequireCustomer)]
public sealed class OrderProductionController(PrintGridDbContext db) : ControllerBase
{
    [HttpGet("{orderId:guid}/production")]
    public async Task<IActionResult> Progress(Guid orderId, CancellationToken ct)
    {
        var customerId = User.GetCustomerId();
        if (customerId is null) return Forbid();
        var order = await db.Set<Order>().AsNoTracking().Include(o => o.Items)
            .SingleOrDefaultAsync(o => o.Id == orderId && o.CustomerId == customerId, ct);
        if (order is null) return NotFound();
        var ids = order.Items.Select(i => i.Id).ToList();
        // Customer progress deliberately contains no lab identity or machine information.
        var batches = await db.Set<Job>().AsNoTracking().Where(j => ids.Contains(j.OrderItemId) && j.Status != JobStatus.Cancelled)
            .OrderBy(j => j.CreatedAt).Select(j => new { j.Id, j.OrderItemId, j.Quantity, j.Status,
                j.ReprintIndex, j.PlannedEndUtc }).ToListAsync(ct);
        return Ok(order.Items.Select(item => new { item.Id, item.Quantity,
            completedQuantity = batches.Where(j => j.OrderItemId == item.Id && j.Status == JobStatus.Completed).Sum(j => j.Quantity),
            batches = batches.Where(j => j.OrderItemId == item.Id).Select(j => new { j.Id, j.Quantity,
                status = j.Status.ToString(), j.ReprintIndex, j.PlannedEndUtc }) }));
    }
}
