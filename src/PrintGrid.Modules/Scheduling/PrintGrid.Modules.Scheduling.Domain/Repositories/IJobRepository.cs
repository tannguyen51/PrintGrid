using PrintGrid.Modules.Scheduling.Domain.Entities;

namespace PrintGrid.Modules.Scheduling.Domain.Repositories;

public interface IJobRepository
{
    Task<Job?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Job>> GetPendingAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Job>> GetActiveByMachineAsync(Guid machineId, CancellationToken cancellationToken = default);
    Task AddAsync(Job job, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<Job> jobs, CancellationToken cancellationToken = default);
}
