using Microsoft.EntityFrameworkCore;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Scheduling.Domain.Repositories;

namespace PrintGrid.Modules.Scheduling.Infrastructure.Persistence.Repositories;

public class JobRepository : IJobRepository
{
    private static readonly JobStatus[] ActiveStatuses =
    {
        JobStatus.Assigned, JobStatus.Accepted, JobStatus.InProgress
    };

    private readonly PrintGridDbContext _context;

    public JobRepository(PrintGridDbContext context) => _context = context;

    public Task<Job?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Set<Job>().FirstOrDefaultAsync(j => j.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Job>> GetPendingAsync(CancellationToken cancellationToken = default) =>
        await _context.Set<Job>()
            .Where(j => j.Status == JobStatus.Pending || j.Status == JobStatus.Reassigned)
            .OrderBy(j => j.InternalDueDate)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Job>> GetByStatusAsync(
        JobStatus status,
        CancellationToken cancellationToken = default) =>
        await _context.Set<Job>()
            .Where(j => j.Status == status)
            .OrderBy(j => j.InternalDueDate)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Job>> GetActiveByMachineAsync(
        Guid machineId,
        CancellationToken cancellationToken = default) =>
        await _context.Set<Job>()
            .Where(j => j.MachineId == machineId && ActiveStatuses.Contains(j.Status))
            .OrderBy(j => j.PlannedStartUtc)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Job job, CancellationToken cancellationToken = default) =>
        await _context.Set<Job>().AddAsync(job, cancellationToken);

    public async Task AddRangeAsync(IEnumerable<Job> jobs, CancellationToken cancellationToken = default) =>
        await _context.Set<Job>().AddRangeAsync(jobs, cancellationToken);
}
