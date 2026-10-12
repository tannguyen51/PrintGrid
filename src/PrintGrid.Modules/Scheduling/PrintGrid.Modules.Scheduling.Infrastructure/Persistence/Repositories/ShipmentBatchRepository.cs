using Microsoft.EntityFrameworkCore;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Repositories;

namespace PrintGrid.Modules.Scheduling.Infrastructure.Persistence.Repositories;

public sealed class ShipmentBatchRepository(PrintGridDbContext context) : IShipmentBatchRepository
{
    public Task AddAsync(ShipmentBatch batch, CancellationToken cancellationToken = default) =>
        context.Set<ShipmentBatch>().AddAsync(batch, cancellationToken).AsTask();

    public async Task<IReadOnlyList<ShipmentBatch>> GetByLabAsync(Guid labId, CancellationToken cancellationToken = default) =>
        await context.Set<ShipmentBatch>().AsNoTracking()
            .Where(batch => batch.LabId == labId)
            .OrderByDescending(batch => batch.SealedAtUtc)
            .ToListAsync(cancellationToken);
}
