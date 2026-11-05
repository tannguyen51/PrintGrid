using Microsoft.EntityFrameworkCore;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Customer.Domain.Entities;
using PrintGrid.Modules.Customer.Domain.Enums;
using PrintGrid.Modules.Customer.Domain.Repositories;

namespace PrintGrid.Modules.Customer.Infrastructure.Persistence.Repositories;

public abstract class ReprintRequestRepository(PrintGridDbContext context) : IReprintRequestRepository
{
    public Task<bool> HasOpenRequestAsync(Guid orderId, CancellationToken cancellationToken = default) =>
        context.Set<ReprintRequest>().AnyAsync(x => x.OrderId == orderId &&
            (x.Status == ReprintRequestStatus.UnderReview || x.Status == ReprintRequestStatus.Approved), cancellationToken);

    public async Task<IReadOnlyList<ReprintRequest>> GetByOrderAsync(Guid orderId, Guid customerId, CancellationToken cancellationToken = default) =>
        await context.Set<ReprintRequest>().Where(x => x.OrderId == orderId && x.CustomerId == customerId)
            .OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);

    public async Task AddAsync(ReprintRequest request, CancellationToken cancellationToken = default) =>
        await context.Set<ReprintRequest>().AddAsync(request, cancellationToken);
}
