using Microsoft.EntityFrameworkCore;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Customer.Domain.Repositories;

namespace PrintGrid.Modules.Customer.Infrastructure.Persistence.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly PrintGridDbContext _context;

    public CustomerRepository(PrintGridDbContext context) => _context = context;

    public Task<Domain.Entities.Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Set<Domain.Entities.Customer>().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<Domain.Entities.Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return _context.Set<Domain.Entities.Customer>()
            .FirstOrDefaultAsync(c => c.Email == normalized, cancellationToken);
    }

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return _context.Set<Domain.Entities.Customer>()
            .AnyAsync(c => c.Email == normalized, cancellationToken);
    }

    public async Task AddAsync(Domain.Entities.Customer customer, CancellationToken cancellationToken = default) =>
        await _context.Set<Domain.Entities.Customer>().AddAsync(customer, cancellationToken);
}
