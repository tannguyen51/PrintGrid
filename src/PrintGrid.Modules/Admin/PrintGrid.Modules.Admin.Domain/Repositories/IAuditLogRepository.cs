using PrintGrid.Modules.Admin.Domain.Entities;

namespace PrintGrid.Modules.Admin.Domain.Repositories;

public sealed record AuditLogFilter(
    string? Actor,
    string? ActionClass,
    DateTime? FromUtc,
    DateTime? ToUtc,
    int Page,
    int PageSize);

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog entry, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<AuditLog> Items, int Total)> SearchAsync(
        AuditLogFilter filter,
        CancellationToken cancellationToken = default);
}
