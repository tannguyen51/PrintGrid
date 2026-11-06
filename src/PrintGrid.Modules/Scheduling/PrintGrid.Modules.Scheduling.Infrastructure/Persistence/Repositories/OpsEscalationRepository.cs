using Microsoft.EntityFrameworkCore;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Scheduling.Domain.Repositories;

namespace PrintGrid.Modules.Scheduling.Infrastructure.Persistence.Repositories;

public class OpsEscalationRepository(PrintGridDbContext context) : IOpsEscalationRepository
{
    public async Task AddAsync(OpsEscalation escalation, CancellationToken cancellationToken = default) =>
        await context.Set<OpsEscalation>().AddAsync(escalation, cancellationToken);

    public async Task<IReadOnlyList<OpsEscalation>> GetOpenAsync(CancellationToken cancellationToken = default) =>
        await context.Set<OpsEscalation>()
            .Where(e => e.Status == OpsEscalationStatus.Open)
            .OrderBy(e => e.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public Task<OpsEscalation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Set<OpsEscalation>().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
}
