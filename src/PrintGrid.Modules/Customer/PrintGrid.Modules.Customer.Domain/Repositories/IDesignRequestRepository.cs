using PrintGrid.Modules.Customer.Domain.Entities;

namespace PrintGrid.Modules.Customer.Domain.Repositories;

public interface IDesignRequestRepository
{
    Task AddAsync(DesignRequest request, CancellationToken cancellationToken = default);
    Task<DesignRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DesignRequest>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DesignRequest>> GetAllAsync(string? status, CancellationToken cancellationToken = default);
}
