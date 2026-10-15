using Microsoft.EntityFrameworkCore;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Admin.Domain.Entities;
using PrintGrid.Modules.Admin.Domain.Repositories;

namespace PrintGrid.Modules.Admin.Infrastructure.Persistence.Repositories;

public sealed class QcKnowledgeRepository(PrintGridDbContext context) : IQcKnowledgeRepository
{
    public async Task<IReadOnlyList<InspectionChecklistVersion>> GetChecklistVersionsAsync(string? qualityGradeCode, CancellationToken cancellationToken = default)
    {
        var query = context.Set<InspectionChecklistVersion>().AsNoTracking();
        if (!string.IsNullOrWhiteSpace(qualityGradeCode)) query = query.Where(x => x.QualityGradeCode == qualityGradeCode);
        return await query.OrderBy(x => x.QualityGradeCode).ThenByDescending(x => x.VersionNumber).ToListAsync(cancellationToken);
    }

    public Task<InspectionChecklistVersion?> GetActiveChecklistAsync(string qualityGradeCode, CancellationToken cancellationToken = default) =>
        context.Set<InspectionChecklistVersion>().FirstOrDefaultAsync(x => x.QualityGradeCode == qualityGradeCode && x.IsActive, cancellationToken);

    public Task<InspectionChecklistVersion?> GetChecklistAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Set<InspectionChecklistVersion>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<int> GetNextChecklistVersionAsync(string qualityGradeCode, CancellationToken cancellationToken = default) =>
        (await context.Set<InspectionChecklistVersion>().Where(x => x.QualityGradeCode == qualityGradeCode)
            .MaxAsync(x => (int?)x.VersionNumber, cancellationToken) ?? 0) + 1;

    public Task<bool> QualityGradeExistsAsync(string qualityGradeCode, CancellationToken cancellationToken = default) =>
        context.Set<SharedCatalogItem>().AnyAsync(x => x.CatalogType == SharedCatalogTypes.QualityGrade &&
            x.Code == qualityGradeCode && x.IsCurrent && !x.IsDeprecated, cancellationToken);

    public async Task AddChecklistVersionAsync(InspectionChecklistVersion version, InspectionChecklistVersion? previousActive, CancellationToken cancellationToken = default)
    {
        previousActive?.Supersede();
        await context.Set<InspectionChecklistVersion>().AddAsync(version, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DefectTaxonomyItem>> GetDefectsAsync(bool includeDeprecated, CancellationToken cancellationToken = default)
    {
        var query = context.Set<DefectTaxonomyItem>().AsNoTracking();
        if (!includeDeprecated) query = query.Where(x => !x.IsDeprecated);
        return await query.OrderBy(x => x.Name).ToListAsync(cancellationToken);
    }

    public Task<DefectTaxonomyItem?> GetDefectAsync(string code, CancellationToken cancellationToken = default) =>
        context.Set<DefectTaxonomyItem>().FirstOrDefaultAsync(x => x.Code == code, cancellationToken);

    public async Task AddDefectAsync(DefectTaxonomyItem item, CancellationToken cancellationToken = default)
    {
        await context.Set<DefectTaxonomyItem>().AddAsync(item, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public Task SaveDefectAsync(CancellationToken cancellationToken = default) => context.SaveChangesAsync(cancellationToken);

    public Task<bool> IsDefectInUseAsync(string code, CancellationToken cancellationToken = default) =>
        context.Database.SqlQueryRaw<bool>("SELECT EXISTS (SELECT 1 FROM scheduling.jobs WHERE inspection_defect_code = {0}) AS \"Value\"", code)
            .SingleAsync(cancellationToken);

    public async Task DeleteDefectAsync(DefectTaxonomyItem item, CancellationToken cancellationToken = default)
    {
        context.Set<DefectTaxonomyItem>().Remove(item);
        await context.SaveChangesAsync(cancellationToken);
    }
}
