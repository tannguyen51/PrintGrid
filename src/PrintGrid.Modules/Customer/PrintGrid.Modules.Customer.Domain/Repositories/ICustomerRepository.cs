namespace PrintGrid.Modules.Customer.Domain.Repositories;

public interface ICustomerRepository
{
    Task<Entities.Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Entities.Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>Every account, for the admin user-management screen. Read-only, ordered by email.</summary>
    Task<IReadOnlyList<Entities.Customer>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Entities.Customer customer, CancellationToken cancellationToken = default);
    void Update(Entities.Customer customer);
}
