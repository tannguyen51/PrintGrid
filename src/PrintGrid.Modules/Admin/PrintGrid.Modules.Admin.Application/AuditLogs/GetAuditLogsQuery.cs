using MediatR;
using PrintGrid.Modules.Admin.Domain.Entities;
using PrintGrid.Modules.Admin.Domain.Repositories;

namespace PrintGrid.Modules.Admin.Application.AuditLogs;

public sealed record GetAuditLogsQuery(
    string? Actor,
    string? ActionClass,
    DateTime? FromUtc,
    DateTime? ToUtc,
    int Page = 1,
    int PageSize = 25) : IRequest<AuditLogPageDto>;

public sealed class GetAuditLogsQueryHandler(IAuditLogRepository repository)
    : IRequestHandler<GetAuditLogsQuery, AuditLogPageDto>
{
    public async Task<AuditLogPageDto> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 10_000);
        var (items, total) = await repository.SearchAsync(
            new AuditLogFilter(request.Actor, request.ActionClass, request.FromUtc, request.ToUtc, page, pageSize),
            cancellationToken);

        return new AuditLogPageDto(items.Select(Map).ToList(), total, page, pageSize);
    }

    private static AuditLogDto Map(AuditLog entry) => new(
        entry.Id,
        entry.ActorId,
        entry.ActorDisplay,
        entry.ActorRole,
        entry.ActionClass,
        entry.Action,
        entry.EntityType,
        entry.EntityId,
        entry.OldValuesJson,
        entry.NewValuesJson,
        entry.Reason,
        entry.IpAddress,
        entry.TraceId,
        entry.CreatedAtUtc);
}
