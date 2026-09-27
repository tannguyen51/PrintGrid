using Microsoft.EntityFrameworkCore;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Customer.Domain.Repositories;

namespace PrintGrid.Modules.Customer.Infrastructure.Persistence.Repositories;

public class ModelRepository : IModelRepository
{
    private readonly PrintGridDbContext _context;

    public ModelRepository(PrintGridDbContext context) => _context = context;

    public async Task<IReadOnlyList<Domain.Entities.Model>> GetForCustomerAsync(
        Guid customerId,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<Domain.Entities.Model>().Where(m => m.CustomerId == customerId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(m =>
                m.Name.ToLower().Contains(term) ||
                (m.Description != null && m.Description.ToLower().Contains(term)));
        }

        return await query
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<Domain.Entities.Model?> GetByIdForCustomerAsync(
        Guid customerId,
        Guid modelId,
        CancellationToken cancellationToken = default) =>
        _context.Set<Domain.Entities.Model>()
            .FirstOrDefaultAsync(m => m.CustomerId == customerId && m.Id == modelId, cancellationToken);

    public Task<Domain.Entities.Model?> GetByIdAsync(
        Guid modelId,
        CancellationToken cancellationToken = default) =>
        _context.Set<Domain.Entities.Model>()
            .FirstOrDefaultAsync(m => m.Id == modelId, cancellationToken);

    public async Task AddAsync(Domain.Entities.Model model, CancellationToken cancellationToken = default) =>
        await _context.Set<Domain.Entities.Model>().AddAsync(model, cancellationToken);

    public void Update(Domain.Entities.Model model) =>
        _context.Set<Domain.Entities.Model>().Update(model);

    public void Remove(Domain.Entities.Model model) =>
        _context.Set<Domain.Entities.Model>().Remove(model);
}