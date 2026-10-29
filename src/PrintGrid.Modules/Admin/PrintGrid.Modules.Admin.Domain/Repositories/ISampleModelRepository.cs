using PrintGrid.Modules.Admin.Domain.Entities;

namespace PrintGrid.Modules.Admin.Domain.Repositories;

public interface ISampleModelRepository
{
    /// <summary>Public library: published samples only, newest curation order first.</summary>
    Task<IReadOnlyList<SampleModel>> GetPublishedAsync(
        string? search,
        string? category,
        CancellationToken cancellationToken = default);

    Task<SampleModel?> GetPublishedBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<SampleModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<SampleModel?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SampleModel>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<bool> SlugExistsAsync(string slug, Guid? exceptId = null, CancellationToken cancellationToken = default);

    Task AddAsync(SampleModel sample, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    void Remove(SampleModel sample);
}
