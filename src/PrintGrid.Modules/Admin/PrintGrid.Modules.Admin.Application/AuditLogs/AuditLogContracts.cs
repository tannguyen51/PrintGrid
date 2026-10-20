namespace PrintGrid.Modules.Admin.Application.AuditLogs;

public static class AuditActionClasses
{
    public const string Authentication = "authentication";
    public const string Configuration = "configuration";
    public const string ManualIntervention = "manual_intervention";
    public const string ModelAccess = "model_access";
    public const string Export = "export";
}

public sealed record AuditLogWriteRequest(
    Guid? ActorId,
    string ActorDisplay,
    string ActorRole,
    string ActionClass,
    string Action,
    string EntityType,
    string? EntityId = null,
    object? OldValues = null,
    object? NewValues = null,
    string? Reason = null,
    string? IpAddress = null,
    string? TraceId = null,
    IReadOnlyList<string>? RequirementCodes = null);

public interface IAuditLogWriter
{
    Task WriteAsync(AuditLogWriteRequest request, CancellationToken cancellationToken = default);
}

public sealed record AuditLogDto(
    Guid Id,
    Guid? ActorId,
    string ActorDisplay,
    string ActorRole,
    string ActionClass,
    string Action,
    string EntityType,
    string? EntityId,
    string? OldValuesJson,
    string? NewValuesJson,
    string? Reason,
    string? IpAddress,
    string? TraceId,
    DateTime CreatedAtUtc);

public sealed record AuditLogPageDto(IReadOnlyList<AuditLogDto> Items, int Total, int Page, int PageSize);
