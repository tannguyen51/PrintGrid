using PrintGrid.Modules.Scheduling.Domain.Entities;

namespace PrintGrid.Modules.Scheduling.Domain.Repositories;

public interface IDateChangeRequestRepository
{
    Task AddAsync(DateChangeRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DateChangeRequest>> GetByJobIdAsync(Guid jobId, CancellationToken cancellationToken = default);

    Task<DateChangeRequest?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
}
