using Microsoft.EntityFrameworkCore;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Customer.Domain.Repositories;

namespace PrintGrid.Modules.Customer.Infrastructure.Persistence.Repositories;

public abstract class ModelRepository(PrintGridDbContext context) : IModelRepository
{
    public async Task<IReadOnlyList<Domain.Entities.Model>> GetForCustomerAsync(
        Guid customerId,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var query = context.Set<Domain.Entities.Model>().Where(m => m.CustomerId == customerId);

        if (string.IsNullOrWhiteSpace(search))
        {
            return await query
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(m =>
                m.Name.Contains(term, StringComparison.CurrentCultureIgnoreCase) ||
                (m.Description != null && m.Description.Contains(term, StringComparison.CurrentCultureIgnoreCase)));
        }

        return await query
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<Domain.Entities.Model?> GetByIdForCustomerAsync(
        Guid customerId,
        Guid modelId,
        CancellationToken cancellationToken = default) =>
        context.Set<Domain.Entities.Model>()
            .FirstOrDefaultAsync(m => m.CustomerId == customerId && m.Id == modelId, cancellationToken);

    public Task<Domain.Entities.Model?> GetByIdAsync(
        Guid modelId,
        CancellationToken cancellationToken = default) =>
        context.Set<Domain.Entities.Model>()
            .FirstOrDefaultAsync(m => m.Id == modelId, cancellationToken);

    public async Task<IReadOnlyList<Domain.Entities.Model>> GetForReviewAsync(
        string? status,
        string? search,
        CancellationToken cancellationToken = default)
    {
        // Only models with a stored file can be reviewed; metadata-only rows are
        // excluded and can never be approved (they cannot be printed either).
        var query = context.Set<Domain.Entities.Model>()
            .Where(m => m.StorageKey != null);

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<Domain.Enums.ModelReviewStatus>(status, ignoreCase: true, out var parsed))
        {
            query = query.Where(m => m.ReviewStatus == parsed);
        }

        if (string.IsNullOrWhiteSpace(search))
        {
            return await query
                .OrderBy(m => m.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(m =>
                m.Name.Contains(term, StringComparison.CurrentCultureIgnoreCase) ||
                m.FileName.Contains(term, StringComparison.CurrentCultureIgnoreCase));
        }

        return await query
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<(int ModelCount, long UsedBytes)> GetStorageUsageAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var query = context.Set<Domain.Entities.Model>().Where(m => m.CustomerId == customerId);
        return (
            await query.CountAsync(cancellationToken),
            await query.SumAsync(m => (long?)m.SizeBytes, cancellationToken) ?? 0L);
    }

    public async Task AddAsync(Domain.Entities.Model model, CancellationToken cancellationToken = default) =>
        await context.Set<Domain.Entities.Model>().AddAsync(model, cancellationToken);

    public void Update(Domain.Entities.Model model) =>
        context.Set<Domain.Entities.Model>().Update(model);

    public void Remove(Domain.Entities.Model model) =>
        context.Set<Domain.Entities.Model>().Remove(model);
}
