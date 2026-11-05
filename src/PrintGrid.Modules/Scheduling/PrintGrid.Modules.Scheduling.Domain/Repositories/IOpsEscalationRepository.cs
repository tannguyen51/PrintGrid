using PrintGrid.Modules.Scheduling.Domain.Entities;

namespace PrintGrid.Modules.Scheduling.Domain.Repositories;

public interface IOpsEscalationRepository
{
    Task AddAsync(OpsEscalation escalation, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OpsEscalation>> GetOpenAsync(CancellationToken cancellationToken = default);
}
