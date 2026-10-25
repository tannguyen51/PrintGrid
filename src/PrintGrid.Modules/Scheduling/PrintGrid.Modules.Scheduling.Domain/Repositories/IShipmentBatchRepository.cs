using PrintGrid.Modules.Scheduling.Domain.Entities;

namespace PrintGrid.Modules.Scheduling.Domain.Repositories;

public interface IShipmentBatchRepository
{
    Task AddAsync(ShipmentBatch batch, CancellationToken cancellationToken = default);
    Task<ShipmentBatch?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShipmentBatch>> GetByLabAsync(Guid labId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShipmentBatch>> GetByStatusAsync(
        ShipmentBatchStatus status, CancellationToken cancellationToken = default);
}
