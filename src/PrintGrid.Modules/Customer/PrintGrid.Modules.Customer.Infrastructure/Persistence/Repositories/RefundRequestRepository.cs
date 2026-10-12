using Microsoft.EntityFrameworkCore;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Customer.Domain.Entities;
using PrintGrid.Modules.Customer.Domain.Repositories;

namespace PrintGrid.Modules.Customer.Infrastructure.Persistence.Repositories;

public sealed class RefundRequestRepository(PrintGridDbContext context) : IRefundRequestRepository
{
    public async Task AddAsync(RefundRequest request, CancellationToken cancellationToken = default) =>
        await context.Set<RefundRequest>().AddAsync(request, cancellationToken);

    public Task<bool> ExistsForDateChangeAsync(Guid dateChangeRequestId, CancellationToken cancellationToken = default) =>
        context.Set<RefundRequest>().AnyAsync(x => x.DateChangeRequestId == dateChangeRequestId, cancellationToken);
}
