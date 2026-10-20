using Microsoft.EntityFrameworkCore;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Admin.Domain.Entities;
using PrintGrid.Modules.Admin.Domain.Repositories;

namespace PrintGrid.Modules.Admin.Infrastructure.Persistence.Repositories;

public sealed class AuditLogRepository(PrintGridDbContext context) : IAuditLogRepository
{
    public async Task AddAsync(AuditLog entry, CancellationToken cancellationToken = default) =>
        await context.Set<AuditLog>().AddAsync(entry, cancellationToken);

    public async Task<(IReadOnlyList<AuditLog> Items, int Total)> SearchAsync(
        AuditLogFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = context.Set<AuditLog>().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Actor))
        {
            var actor = filter.Actor.Trim().ToLower();
            query = query.Where(x => x.ActorDisplay.ToLower().Contains(actor));
        }
        if (!string.IsNullOrWhiteSpace(filter.ActionClass))
            query = query.Where(x => x.ActionClass == filter.ActionClass.Trim());
        if (filter.FromUtc is not null)
            query = query.Where(x => x.CreatedAtUtc >= filter.FromUtc.Value);
        if (filter.ToUtc is not null)
            query = query.Where(x => x.CreatedAtUtc <= filter.ToUtc.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.Id)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);
        return (items, total);
    }
}
