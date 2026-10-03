namespace PrintGrid.SharedKernel.Interfaces;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits pending changes; returns <c>false</c> (discarding/rolling back the tracked
    /// changes) when a database constraint rejects them — e.g. the machine-slot exclusion
    /// constraint losing a placement race (NFR-REL-004). Callers can then pick the next
    /// candidate and retry instead of surfacing a 500.
    /// </summary>
    Task<bool> TrySaveChangesSafeAsync(CancellationToken cancellationToken = default);
}
