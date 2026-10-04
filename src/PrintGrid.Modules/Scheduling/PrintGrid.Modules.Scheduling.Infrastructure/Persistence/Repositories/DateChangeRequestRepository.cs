using Microsoft.EntityFrameworkCore;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
namespace PrintGrid.Modules.Scheduling.Infrastructure.Persistence.Repositories;
public class DateChangeRequestRepository(PrintGridDbContext db) : IDateChangeRequestRepository
{
    public async Task AddAsync(DateChangeRequest request, CancellationToken ct = default) => await db.Set<DateChangeRequest>().AddAsync(request, ct);
    public Task<bool> HasPendingAsync(Guid jobId, CancellationToken ct = default) => db.Set<DateChangeRequest>().AnyAsync(x => x.JobId == jobId && x.Status == "Pending", ct);
}
