using Microsoft.EntityFrameworkCore;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Customer.Domain.Repositories;

namespace PrintGrid.Modules.Customer.Infrastructure.Persistence.Repositories;

public class CustomerRepository(PrintGridDbContext context) : ICustomerRepository
{
    public Task<Domain.Entities.Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Set<Domain.Entities.Customer>().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<Domain.Entities.Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return context.Set<Domain.Entities.Customer>()
            .FirstOrDefaultAsync(c => c.Email == normalized, cancellationToken);
    }

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return context.Set<Domain.Entities.Customer>()
            .AnyAsync(c => c.Email == normalized, cancellationToken);
    }

    public Task<Domain.Entities.Customer?> GetByGoogleSubjectAsync(string googleSubject, CancellationToken cancellationToken = default) =>
        context.Set<Domain.Entities.Customer>()
            .FirstOrDefaultAsync(c => c.GoogleSubject == googleSubject, cancellationToken);

    public async Task<IReadOnlyList<Domain.Entities.Customer>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.Set<Domain.Entities.Customer>()
            .AsNoTracking()
            .OrderBy(c => c.Email)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Domain.Entities.Customer customer, CancellationToken cancellationToken = default) =>
        await context.Set<Domain.Entities.Customer>().AddAsync(customer, cancellationToken);

    public void Update(Domain.Entities.Customer customer) => context.Set<Domain.Entities.Customer>().Update(customer);
}
