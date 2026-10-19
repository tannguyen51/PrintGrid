using Microsoft.EntityFrameworkCore;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Customer.Domain.Entities;
using PrintGrid.Modules.Customer.Domain.Repositories;

namespace PrintGrid.Modules.Customer.Infrastructure.Persistence.Repositories;

public sealed class RefundRequestRepository(PrintGridDbContext context) : IRefundRequestRepository
{
    public async Task AddAsync(RefundRequest request, CancellationToken cancellationToken = default) =>
        await context.Set<RefundRequest>().AddAsync(request, cancellationToken);

    public Task<bool> ExistsForDateChangeAsync(Guid dateChangeRequestId, CancellationToken cancellationToken = default) =>
        context.Set<RefundRequest>().AnyAsync(x => x.DateChangeRequestId == dateChangeRequestId, cancellationToken);

    public Task<RefundRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Set<RefundRequest>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<RefundRequest>> GetPendingAsync(CancellationToken cancellationToken = default) =>
        await GetByStatusAsync("Pending", cancellationToken);

    public async Task<IReadOnlyList<RefundRequest>> GetByStatusAsync(string? status, CancellationToken cancellationToken = default)
    {
        var query = context.Set<RefundRequest>().AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(x => x.Status == status.Trim());

        return await query
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }
}
