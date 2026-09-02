using PrintGrid.SharedKernel.Interfaces;

namespace PrintGrid.Infrastructure.Shared.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly PrintGridDbContext _context;

    public UnitOfWork(PrintGridDbContext context) => _context = context;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
