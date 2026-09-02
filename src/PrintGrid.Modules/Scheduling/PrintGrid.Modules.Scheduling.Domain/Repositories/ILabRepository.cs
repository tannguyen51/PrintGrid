using PrintGrid.Modules.Scheduling.Domain.Entities;

namespace PrintGrid.Modules.Scheduling.Domain.Repositories;

public interface ILabRepository
{
    Task<Lab?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Lab>> GetActiveWithMachinesAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Lab lab, CancellationToken cancellationToken = default);
}
