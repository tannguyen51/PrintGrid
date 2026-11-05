using Microsoft.EntityFrameworkCore;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Customer.Domain.Entities;
using PrintGrid.Modules.Customer.Domain.Enums;
using PrintGrid.Modules.Customer.Domain.Repositories;

namespace PrintGrid.Modules.Customer.Infrastructure.Persistence.Repositories;

public abstract class OrderRepository(PrintGridDbContext context) : IOrderRepository
{
    public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Set<Order>()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Order>> GetByCustomerAsync(
        Guid customerId,
        CancellationToken cancellationToken = default) =>
        await context.Set<Order>()
            .Include(o => o.Items)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Order>> GetByStatusAsync(
        IReadOnlyCollection<OrderStatus> statuses,
        CancellationToken cancellationToken = default) =>
        await context.Set<Order>()
            .Include(o => o.Items)
            .Where(o => statuses.Contains(o.Status))
            .OrderBy(o => o.PromisedDeliveryDate)
            .ToListAsync(cancellationToken);

    public Task<Order?> GetByItemIdAsync(Guid orderItemId, CancellationToken cancellationToken = default) =>
        context.Set<Order>()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Items.Any(i => i.Id == orderItemId), cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        context.Set<Order>().CountAsync(cancellationToken);

    public async Task AddAsync(Order order, CancellationToken cancellationToken = default) =>
        await context.Set<Order>().AddAsync(order, cancellationToken);
}
