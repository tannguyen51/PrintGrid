using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;

namespace PrintGrid.Modules.Scheduling.Domain.Repositories;

public interface IJobRepository
{
    Task<Job?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Job>> GetPendingAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Job>> GetByStatusAsync(JobStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Job>> GetActiveByMachineAsync(Guid machineId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Job>> GetByLabIdAsync(Guid labId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Job>> GetAllAssignedToLabsAsync(DateTime sinceUtc, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<Guid, int>> GetActiveCountsByLabAsync(CancellationToken cancellationToken = default);
    Task<bool> HasActiveForMachineAsync(Guid machineId, CancellationToken cancellationToken = default);
    Task AddAsync(Job job, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<Job> jobs, CancellationToken cancellationToken = default);
}
