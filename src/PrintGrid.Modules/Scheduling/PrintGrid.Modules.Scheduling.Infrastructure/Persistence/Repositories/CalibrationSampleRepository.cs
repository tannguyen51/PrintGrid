using Microsoft.EntityFrameworkCore;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Repositories;

namespace PrintGrid.Modules.Scheduling.Infrastructure.Persistence.Repositories;

public class CalibrationSampleRepository : ICalibrationSampleRepository
{
    private readonly PrintGridDbContext _context;

    public CalibrationSampleRepository(PrintGridDbContext context) => _context = context;

    public async Task<IReadOnlyList<CalibrationSample>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.Set<CalibrationSample>()
            .AsNoTracking()
            .OrderBy(s => s.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CalibrationSample>> GetByMachineIdAsync(
        Guid machineId,
        CancellationToken cancellationToken = default) =>
        await _context.Set<CalibrationSample>()
            .AsNoTracking()
            .Where(s => s.MachineId == machineId)
            .OrderBy(s => s.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlySet<Guid>> GetSampledJobIdsAsync(
        IReadOnlyCollection<Guid> jobIds,
        CancellationToken cancellationToken = default)
    {
        if (jobIds.Count == 0) return new HashSet<Guid>();

        var sampled = await _context.Set<CalibrationSample>()
            .AsNoTracking()
            .Where(s => jobIds.Contains(s.JobId))
            .Select(s => s.JobId)
            .ToListAsync(cancellationToken);
        return sampled.ToHashSet();
    }

    public async Task AddRangeAsync(IEnumerable<CalibrationSample> samples, CancellationToken cancellationToken = default) =>
        await _context.Set<CalibrationSample>().AddRangeAsync(samples, cancellationToken);
}
