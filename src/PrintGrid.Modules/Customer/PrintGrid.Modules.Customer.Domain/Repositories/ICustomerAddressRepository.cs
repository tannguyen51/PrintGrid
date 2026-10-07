using PrintGrid.Modules.Customer.Domain.Entities;

namespace PrintGrid.Modules.Customer.Domain.Repositories;

public interface ICustomerAddressRepository
{
    Task<IReadOnlyList<CustomerAddress>> ListAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<CustomerAddress?> GetAsync(Guid customerId, Guid addressId, CancellationToken cancellationToken = default);
    Task<bool> HasAnyAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<bool> IsUsedByOpenOrderAsync(CustomerAddress address, CancellationToken cancellationToken = default);
    Task AddAsync(CustomerAddress address, CancellationToken cancellationToken = default);
    void Remove(CustomerAddress address);
}
