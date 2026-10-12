using PrintGrid.Modules.Scheduling.Domain.Entities;

namespace PrintGrid.Modules.Scheduling.Domain.Repositories;

public interface IScoringWeightConfigurationRepository
{
    Task<ScoringWeightConfiguration?> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ScoringWeightConfiguration>> GetHistoryAsync(CancellationToken cancellationToken = default);
    Task<bool> VersionExistsAsync(string version, CancellationToken cancellationToken = default);
    Task AddAndActivateAsync(ScoringWeightConfiguration configuration, CancellationToken cancellationToken = default);
}
