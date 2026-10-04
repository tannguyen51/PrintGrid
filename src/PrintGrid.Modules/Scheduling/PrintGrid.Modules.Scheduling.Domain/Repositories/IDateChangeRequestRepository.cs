using PrintGrid.Modules.Scheduling.Domain.Entities;
namespace PrintGrid.Modules.Scheduling.Domain.Repositories;
public interface IDateChangeRequestRepository
{
    Task AddAsync(DateChangeRequest request, CancellationToken ct = default);
    Task<bool> HasPendingAsync(Guid jobId, CancellationToken ct = default);
}
