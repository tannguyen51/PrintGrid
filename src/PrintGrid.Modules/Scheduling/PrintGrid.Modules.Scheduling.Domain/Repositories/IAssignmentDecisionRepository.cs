using PrintGrid.Modules.Scheduling.Domain.Entities;

namespace PrintGrid.Modules.Scheduling.Domain.Repositories;

/// <summary>
/// Append-only store for assignment decisions (FR-SCHED-009). Deliberately has no update or
/// delete member: audit rows are immutable (BR-CONFIG-004), and no API may mutate them.
/// </summary>
public interface IAssignmentDecisionRepository
{
    Task AddAsync(AssignmentDecision decision, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AssignmentDecision>> GetByJobIdAsync(Guid jobId, CancellationToken cancellationToken = default);
}
