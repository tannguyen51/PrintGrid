using Microsoft.EntityFrameworkCore;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Scheduling.Domain.Repositories;

namespace PrintGrid.Modules.Scheduling.Infrastructure.Persistence.Repositories;

public class LabRepository : ILabRepository
{
    private readonly PrintGridDbContext _context;

    public LabRepository(PrintGridDbContext context) => _context = context;

    public Task<Lab?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Set<Lab>()
            .Include(l => l.Machines)
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Lab>> GetActiveWithMachinesAsync(CancellationToken cancellationToken = default) =>
        await _context.Set<Lab>()
            .Include(l => l.Machines)
            .Where(l => l.IsActive)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Lab>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.Set<Lab>()
            .Include(l => l.Machines)
            .OrderBy(l => l.Name)
            .ToListAsync(cancellationToken);

    public Task<bool> AnyAsync(CancellationToken cancellationToken = default) =>
        _context.Set<Lab>().AnyAsync(cancellationToken);

    public async Task AddAsync(Lab lab, CancellationToken cancellationToken = default) =>
        await _context.Set<Lab>().AddAsync(lab, cancellationToken);

    public async Task<NetworkStatsSnapshot> GetNetworkStatsAsync(CancellationToken cancellationToken = default)
    {
        var labs = await _context.Set<Lab>().Include(l => l.Machines).AsNoTracking().ToListAsync(cancellationToken);

        var active = labs.Where(l => l.IsActive).ToList();
        var machines = active.SelectMany(l => l.Machines).ToList();

        return new NetworkStatsSnapshot(
            TotalLabs: labs.Count,
            ActiveLabs: active.Count,
            TotalMachines: machines.Count,
            FdmMachines: machines.Count(m => m.Technology == PrintTechnology.Fdm),
            SlaMachines: machines.Count(m => m.Technology == PrintTechnology.Sla),
            SlsMachines: machines.Count(m => m.Technology == PrintTechnology.Sls),
            AverageOnTimeDeliveryRate: active.Count == 0 ? 0m : active.Average(l => l.OnTimeDeliveryRate),
            AverageFirstPassYield: active.Count == 0 ? 0m : active.Average(l => l.FirstPassYield));
    }
}
