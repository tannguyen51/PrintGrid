using Microsoft.EntityFrameworkCore;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Scheduling.Application.Abstractions;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.Modules.Scheduling.Domain.Services;

namespace PrintGrid.Modules.Scheduling.Infrastructure.Persistence.Repositories;

public sealed class ScoringWeightConfigurationRepository(PrintGridDbContext context)
    : IScoringWeightConfigurationRepository, IScoringConfigurationProvider
{
    public Task<ScoringWeightConfiguration?> GetActiveAsync(CancellationToken cancellationToken = default) =>
        context.Set<ScoringWeightConfiguration>().AsNoTracking()
            .SingleOrDefaultAsync(x => x.IsActive, cancellationToken);

    async Task<ScoringParameterSet> IScoringConfigurationProvider.GetActiveAsync(CancellationToken cancellationToken)
    {
        var active = await GetActiveAsync(cancellationToken);
        return active?.ToParameterSet() ?? ScoringParameterSet.Active;
    }

    public async Task<IReadOnlyList<ScoringWeightConfiguration>> GetHistoryAsync(CancellationToken cancellationToken = default) =>
        await context.Set<ScoringWeightConfiguration>().AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc).ToListAsync(cancellationToken);

    public Task<bool> VersionExistsAsync(string version, CancellationToken cancellationToken = default) =>
        context.Set<ScoringWeightConfiguration>().AnyAsync(x => x.Version == version, cancellationToken);

    public async Task AddAndActivateAsync(ScoringWeightConfiguration configuration, CancellationToken cancellationToken = default)
    {
        var active = await context.Set<ScoringWeightConfiguration>()
            .Where(x => x.IsActive).ToListAsync(cancellationToken);
        foreach (var current in active) current.Deactivate();
        await context.Set<ScoringWeightConfiguration>().AddAsync(configuration, cancellationToken);
    }
}
