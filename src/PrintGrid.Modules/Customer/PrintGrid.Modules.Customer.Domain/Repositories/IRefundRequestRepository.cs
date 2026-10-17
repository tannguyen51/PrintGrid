using PrintGrid.Modules.Customer.Domain.Entities;

namespace PrintGrid.Modules.Customer.Domain.Repositories;

public interface IRefundRequestRepository
{
    Task AddAsync(RefundRequest request, CancellationToken cancellationToken = default);
    Task<bool> ExistsForDateChangeAsync(Guid dateChangeRequestId, CancellationToken cancellationToken = default);
}
