using Microsoft.EntityFrameworkCore;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Customer.Domain.Entities;
using PrintGrid.Modules.Customer.Domain.Enums;
using PrintGrid.Modules.Customer.Domain.Repositories;

namespace PrintGrid.Modules.Customer.Infrastructure.Persistence.Repositories;

public class ReprintRequestRepository : IReprintRequestRepository
{
    private readonly PrintGridDbContext _context;
    public ReprintRequestRepository(PrintGridDbContext context) => _context = context;

    public Task<bool> HasOpenRequestAsync(Guid orderId, CancellationToken cancellationToken = default) =>
        _context.Set<ReprintRequest>().AnyAsync(x => x.OrderId == orderId &&
            (x.Status == ReprintRequestStatus.UnderReview || x.Status == ReprintRequestStatus.Approved), cancellationToken);

    public async Task<IReadOnlyList<ReprintRequest>> GetByOrderAsync(Guid orderId, Guid customerId, CancellationToken cancellationToken = default) =>
        await _context.Set<ReprintRequest>().Where(x => x.OrderId == orderId && x.CustomerId == customerId)
            .OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);

    public async Task AddAsync(ReprintRequest request, CancellationToken cancellationToken = default) =>
        await _context.Set<ReprintRequest>().AddAsync(request, cancellationToken);
}
