using Microsoft.EntityFrameworkCore;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Scheduling.Domain.Entities;
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

    public async Task AddAsync(Lab lab, CancellationToken cancellationToken = default) =>
        await _context.Set<Lab>().AddAsync(lab, cancellationToken);
}
