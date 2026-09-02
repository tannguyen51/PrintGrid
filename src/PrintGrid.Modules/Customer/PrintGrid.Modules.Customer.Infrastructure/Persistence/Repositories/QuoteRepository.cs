using Microsoft.EntityFrameworkCore;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Customer.Domain.Entities;
using PrintGrid.Modules.Customer.Domain.Repositories;

namespace PrintGrid.Modules.Customer.Infrastructure.Persistence.Repositories;

public class QuoteRepository : IQuoteRepository
{
    private readonly PrintGridDbContext _context;

    public QuoteRepository(PrintGridDbContext context) => _context = context;

    public Task<Quote?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Set<Quote>()
            .Include(q => q.Items)
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Quote>> GetByCustomerAsync(
        Guid customerId,
        CancellationToken cancellationToken = default) =>
        await _context.Set<Quote>()
            .Include(q => q.Items)
            .Where(q => q.CustomerId == customerId)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Quote quote, CancellationToken cancellationToken = default) =>
        await _context.Set<Quote>().AddAsync(quote, cancellationToken);
}
