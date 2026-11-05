using Microsoft.EntityFrameworkCore;
using PrintGrid.SharedKernel.Interfaces;

namespace PrintGrid.Infrastructure.Shared.Persistence;

public class UnitOfWork(PrintGridDbContext context) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);

    public async Task<bool> TrySaveChangesSafeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
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
        foreach (var entry in context.ChangeTracker.Entries().ToList())
        {
            entry.State = entry.State switch
            {
                EntityState.Added => EntityState.Detached,
                EntityState.Modified or EntityState.Deleted => EntityState.Detached,
                _ => entry.State
            };
        }
    }
}
