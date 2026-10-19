using PrintGrid.Modules.Customer.Domain.Entities;

namespace PrintGrid.Modules.Customer.Domain.Repositories;

public interface IRefundRequestRepository
{
    Task AddAsync(RefundRequest request, CancellationToken cancellationToken = default);
    Task<bool> ExistsForDateChangeAsync(Guid dateChangeRequestId, CancellationToken cancellationToken = default);
    Task<RefundRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RefundRequest>> GetPendingAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RefundRequest>> GetByStatusAsync(string? status, CancellationToken cancellationToken = default);
}
