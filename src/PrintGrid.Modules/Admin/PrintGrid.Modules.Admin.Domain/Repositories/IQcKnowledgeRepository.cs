using PrintGrid.Modules.Admin.Domain.Entities;

namespace PrintGrid.Modules.Admin.Domain.Repositories;

public interface IQcKnowledgeRepository
{
    Task<IReadOnlyList<InspectionChecklistVersion>> GetChecklistVersionsAsync(string? qualityGradeCode, CancellationToken cancellationToken = default);
    Task<InspectionChecklistVersion?> GetActiveChecklistAsync(string qualityGradeCode, CancellationToken cancellationToken = default);
    Task<InspectionChecklistVersion?> GetChecklistAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> GetNextChecklistVersionAsync(string qualityGradeCode, CancellationToken cancellationToken = default);
    Task<bool> QualityGradeExistsAsync(string qualityGradeCode, CancellationToken cancellationToken = default);
    Task AddChecklistVersionAsync(InspectionChecklistVersion version, InspectionChecklistVersion? previousActive, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DefectTaxonomyItem>> GetDefectsAsync(bool includeDeprecated, CancellationToken cancellationToken = default);
    Task<DefectTaxonomyItem?> GetDefectAsync(string code, CancellationToken cancellationToken = default);
    Task AddDefectAsync(DefectTaxonomyItem item, CancellationToken cancellationToken = default);
    Task SaveDefectAsync(CancellationToken cancellationToken = default);
    Task<bool> IsDefectInUseAsync(string code, CancellationToken cancellationToken = default);
    Task DeleteDefectAsync(DefectTaxonomyItem item, CancellationToken cancellationToken = default);
}
