using PrintGrid.Modules.Scheduling.Domain.Services;

namespace PrintGrid.Modules.Scheduling.Application.Abstractions;

public interface IScoringConfigurationProvider
{
    Task<ScoringParameterSet> GetActiveAsync(CancellationToken cancellationToken = default);
}
