using Microsoft.EntityFrameworkCore;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Admin.Domain.Entities;
using PrintGrid.Modules.Admin.Domain.Repositories;

namespace PrintGrid.Modules.Admin.Infrastructure.Persistence.Repositories;

public sealed class SampleModelRepository(PrintGridDbContext context) : ISampleModelRepository
{
    public async Task<IReadOnlyList<SampleModel>> GetPublishedAsync(
        string? search,
        string? category,
        CancellationToken cancellationToken = default)
    {
        var query = context.Set<SampleModel>().AsNoTracking().Where(x => x.IsPublished);

        if (!string.IsNullOrWhiteSpace(category) && category != "Tất cả")
            query = query.Where(x => x.Category == category);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.Name.ToLower().Contains(term) ||
                (x.Description != null && x.Description.ToLower().Contains(term)) ||
                x.Category.ToLower().Contains(term));
        }

        return await query
            .OrderBy(x => x.SortOrder)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public Task<SampleModel?> GetPublishedBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        context.Set<SampleModel>().AsNoTracking()
            .FirstOrDefaultAsync(x => x.Slug == slug && x.IsPublished, cancellationToken);

    public Task<SampleModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Set<SampleModel>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<SampleModel?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        context.Set<SampleModel>().FirstOrDefaultAsync(x => x.Slug == slug, cancellationToken);

    public async Task<IReadOnlyList<SampleModel>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.Set<SampleModel>().AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public Task<bool> SlugExistsAsync(string slug, Guid? exceptId = null, CancellationToken cancellationToken = default) =>
        context.Set<SampleModel>().AnyAsync(
            x => x.Slug == slug && (exceptId == null || x.Id != exceptId), cancellationToken);

    public async Task AddAsync(SampleModel sample, CancellationToken cancellationToken = default) =>
        await context.Set<SampleModel>().AddAsync(sample, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);

    public void Remove(SampleModel sample) => context.Set<SampleModel>().Remove(sample);
}
