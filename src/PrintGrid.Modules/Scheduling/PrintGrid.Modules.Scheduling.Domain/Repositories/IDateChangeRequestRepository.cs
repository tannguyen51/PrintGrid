using PrintGrid.Modules.Scheduling.Domain.Entities;

namespace PrintGrid.Modules.Scheduling.Domain.Repositories;

public interface IDateChangeRequestRepository
{
    Task AddAsync(DateChangeRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DateChangeRequest>> GetByJobIdAsync(Guid jobId, CancellationToken cancellationToken = default);

    Task<DateChangeRequest?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// True when the job already has a proposal waiting for the customer — the at-risk repair
    /// must not stack a second one (FR-SCHED-007 repair flow).
    /// </summary>
    Task<bool> HasPendingAsync(Guid jobId, CancellationToken cancellationToken = default);
}
