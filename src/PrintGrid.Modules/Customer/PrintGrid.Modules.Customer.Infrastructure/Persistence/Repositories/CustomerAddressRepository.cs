using Microsoft.EntityFrameworkCore;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Customer.Domain.Entities;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.Modules.Customer.Domain.Enums;

namespace PrintGrid.Modules.Customer.Infrastructure.Persistence.Repositories;

public abstract class CustomerAddressRepository(PrintGridDbContext context) : ICustomerAddressRepository
{
    public async Task<IReadOnlyList<CustomerAddress>> ListAsync(Guid customerId, CancellationToken cancellationToken = default) =>
        await context.Set<CustomerAddress>().Where(x => x.CustomerId == customerId)
            .OrderByDescending(x => x.IsDefault).ThenBy(x => x.CreatedAt).ToListAsync(cancellationToken);

    public Task<CustomerAddress?> GetAsync(Guid customerId, Guid addressId, CancellationToken cancellationToken = default) =>
        context.Set<CustomerAddress>().FirstOrDefaultAsync(x => x.Id == addressId && x.CustomerId == customerId, cancellationToken);

    public Task<bool> HasAnyAsync(Guid customerId, CancellationToken cancellationToken = default) =>
        context.Set<CustomerAddress>().AnyAsync(x => x.CustomerId == customerId, cancellationToken);

    public Task<bool> IsUsedByOpenOrderAsync(CustomerAddress address, CancellationToken cancellationToken = default) =>
        context.Set<Order>().AnyAsync(x => x.CustomerId == address.CustomerId
            && x.Status != OrderStatus.Delivered && x.Status != OrderStatus.Cancelled
            && x.DeliveryAddress.Street == address.Street
            && x.DeliveryAddress.Ward == address.Ward
            && x.DeliveryAddress.District == address.District
            && x.DeliveryAddress.City == address.City
            && x.DeliveryAddress.PostalCode == address.PostalCode
            && x.DeliveryAddress.Country == address.Country, cancellationToken);

    public async Task AddAsync(CustomerAddress address, CancellationToken cancellationToken = default) =>
        await context.Set<CustomerAddress>().AddAsync(address, cancellationToken);

    public void Remove(CustomerAddress address) => context.Set<CustomerAddress>().Remove(address);
}
