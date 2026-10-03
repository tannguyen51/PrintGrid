using PrintGrid.Modules.Scheduling.Domain.Entities;

namespace PrintGrid.Modules.Scheduling.Domain.Repositories;

public interface ILabRepository
{
    Task<Lab?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Lab>> GetActiveWithMachinesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Lab>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> AnyAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Lab lab, CancellationToken cancellationToken = default);
    Task<NetworkStatsSnapshot> GetNetworkStatsAsync(CancellationToken cancellationToken = default);
}

public sealed record NetworkStatsSnapshot(
    int TotalLabs,
    int ActiveLabs,
    int TotalMachines,
    int FdmMachines,
    int SlaMachines,
    int SlsMachines,
    decimal AverageOnTimeDeliveryRate,
    decimal AverageFirstPassYield);
