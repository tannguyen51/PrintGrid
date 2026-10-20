using Microsoft.EntityFrameworkCore;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Customer.Domain.Entities;
using PrintGrid.Modules.Customer.Domain.Repositories;

namespace PrintGrid.Modules.Customer.Infrastructure.Persistence.Repositories;

public sealed class DesignRequestRepository(PrintGridDbContext context) : IDesignRequestRepository
{
    public async Task AddAsync(DesignRequest request, CancellationToken cancellationToken = default) =>
        await context.Set<DesignRequest>().AddAsync(request, cancellationToken);

    public Task<DesignRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Set<DesignRequest>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<DesignRequest>> GetByCustomerAsync(
        Guid customerId,
        CancellationToken cancellationToken = default) =>
        await context.Set<DesignRequest>()
            .Where(x => x.CustomerId == customerId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<DesignRequest>> GetAllAsync(
        string? status,
        CancellationToken cancellationToken = default)
    {
        var query = context.Set<DesignRequest>().AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(x => x.Status == status.Trim());

        return await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }
}
