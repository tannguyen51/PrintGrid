using Microsoft.EntityFrameworkCore;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Repositories;

namespace PrintGrid.Modules.Scheduling.Infrastructure.Persistence.Repositories;

public abstract class AssignmentDecisionRepository(PrintGridDbContext context) : IAssignmentDecisionRepository
{
    public async Task AddAsync(AssignmentDecision decision, CancellationToken cancellationToken = default) =>
        await context.Set<AssignmentDecision>().AddAsync(decision, cancellationToken);

    public async Task<IReadOnlyList<AssignmentDecision>> GetByJobIdAsync(
        Guid jobId, CancellationToken cancellationToken = default) =>
        await context.Set<AssignmentDecision>()
            .Where(d => d.JobId == jobId)
            .OrderBy(d => d.CreatedAtUtc)
            .ThenBy(d => d.Id)
            .ToListAsync(cancellationToken);
}
