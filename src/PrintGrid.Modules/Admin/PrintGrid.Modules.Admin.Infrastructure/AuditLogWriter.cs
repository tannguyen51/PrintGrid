using System.Text.Json;
using PrintGrid.Infrastructure.Shared.Persistence;
using PrintGrid.Modules.Admin.Application.AuditLogs;
using PrintGrid.Modules.Admin.Domain.Entities;
using PrintGrid.Modules.Admin.Domain.Repositories;

namespace PrintGrid.Modules.Admin.Infrastructure;

public sealed class AuditLogWriter(IAuditLogRepository repository, PrintGridDbContext context) : IAuditLogWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task WriteAsync(AuditLogWriteRequest request, CancellationToken cancellationToken = default)
    {
        var codes = (request.RequirementCodes ?? ["FR-ADMIN-004", "UC-025", "BR-CONFIG-004"])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToArray();
        var entry = AuditLog.Record(
            request.ActorId,
            request.ActorDisplay,
            request.ActorRole,
            request.ActionClass,
            request.Action,
            request.EntityType,
            request.EntityId,
            Serialize(request.OldValues),
            Serialize(request.NewValues),
            request.Reason,
            request.IpAddress,
            request.TraceId,
            JsonSerializer.Serialize(codes, JsonOptions));
        await repository.AddAsync(entry, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static string? Serialize(object? value) =>
        value is null ? null : JsonSerializer.Serialize(value, JsonOptions);
}
