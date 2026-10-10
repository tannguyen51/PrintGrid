using PrintGrid.Modules.Customer.Domain.Entities;

namespace PrintGrid.Modules.Customer.Domain.Repositories;

public interface IReprintRequestRepository
{
    Task<bool> HasOpenRequestAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReprintRequest>> GetByOrderAsync(Guid orderId, Guid customerId, CancellationToken cancellationToken = default);
    Task AddAsync(ReprintRequest request, CancellationToken cancellationToken = default);
}
