using Microsoft.EntityFrameworkCore;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Repositories;

namespace PrintGrid.Modules.Scheduling.Infrastructure.Persistence.Repositories;

public class DateChangeRequestRepository : IDateChangeRequestRepository
{
    private readonly PrintGridDbContext _context;

    public DateChangeRequestRepository(PrintGridDbContext context) => _context = context;

    public async Task AddAsync(DateChangeRequest request, CancellationToken cancellationToken = default) =>
        await _context.Set<DateChangeRequest>().AddAsync(request, cancellationToken);

    public async Task<IReadOnlyList<DateChangeRequest>> GetByJobIdAsync(
        Guid jobId, CancellationToken cancellationToken = default) =>
        await _context.Set<DateChangeRequest>()
            .Where(r => r.JobId == jobId)
            .OrderBy(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public Task<DateChangeRequest?> GetByTokenAsync(string token, CancellationToken cancellationToken = default) =>
        _context.Set<DateChangeRequest>().FirstOrDefaultAsync(r => r.Token == token, cancellationToken);
}
