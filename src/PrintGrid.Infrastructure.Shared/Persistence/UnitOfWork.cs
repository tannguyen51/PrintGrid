using Microsoft.EntityFrameworkCore;
using PrintGrid.SharedKernel.Interfaces;

namespace PrintGrid.Infrastructure.Shared.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly PrintGridDbContext _context;

    public UnitOfWork(PrintGridDbContext context) => _context = context;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);

    public async Task<bool> TrySaveChangesSafeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            // DB-level guard tripped (e.g. machine-slot EXCLUDE constraint lost a race).
            // Discard the offending changes so the caller can pick the next candidate.
            DiscardPendingChanges();
            return false;
        }
    }

    private void DiscardPendingChanges()
    {
        foreach (var entry in _context.ChangeTracker.Entries().ToList())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.State = EntityState.Detached;
                    break;
                case EntityState.Modified or EntityState.Deleted:
                    entry.State = EntityState.Detached;
                    break; // detached → re-reads come from DB on next load
            }
        }
    }
}
