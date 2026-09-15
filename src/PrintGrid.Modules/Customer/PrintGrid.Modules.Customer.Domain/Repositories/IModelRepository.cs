namespace PrintGrid.Modules.Customer.Domain.Repositories;

public interface IModelRepository
{
    Task<IReadOnlyList<Entities.Model>> GetForCustomerAsync(
        Guid customerId,
        string? search,
        CancellationToken cancellationToken = default);

    Task<Entities.Model?> GetByIdForCustomerAsync(
        Guid customerId,
        Guid modelId,
        CancellationToken cancellationToken = default);

    Task AddAsync(Entities.Model model, CancellationToken cancellationToken = default);
    void Update(Entities.Model model);
    void Remove(Entities.Model model);
}