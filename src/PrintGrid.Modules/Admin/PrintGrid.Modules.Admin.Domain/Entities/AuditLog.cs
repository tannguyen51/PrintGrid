using PrintGrid.SharedKernel.Common;

namespace PrintGrid.Modules.Admin.Domain.Entities;

/// <summary>
/// Immutable system audit evidence (FR-ADMIN-004 / UC-025). Rows are append-only and
/// retained for at least two academic years (BR-CONFIG-004).
/// </summary>
public sealed class AuditLog : Entity<Guid>
{
    public Guid? ActorId { get; private set; }
    public string ActorDisplay { get; private set; } = string.Empty;
    public string ActorRole { get; private set; } = string.Empty;
    public string ActionClass { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty;
    public string? EntityId { get; private set; }
    public string? OldValuesJson { get; private set; }
    public string? NewValuesJson { get; private set; }
    public string? Reason { get; private set; }
    public string? IpAddress { get; private set; }
    public string? TraceId { get; private set; }
    public string RequirementCodesJson { get; private set; } = "[]";
    public DateTime CreatedAtUtc { get; private set; }

    private AuditLog() { }

    public static AuditLog Record(
        Guid? actorId,
        string actorDisplay,
        string actorRole,
        string actionClass,
        string action,
        string entityType,
        string? entityId,
        string? oldValuesJson,
        string? newValuesJson,
        string? reason,
        string? ipAddress,
        string? traceId,
        string requirementCodesJson,
        DateTime? createdAtUtc = null) => new()
        {
            Id = Guid.NewGuid(),
            ActorId = actorId,
            ActorDisplay = actorDisplay.Trim(),
            ActorRole = actorRole.Trim(),
            ActionClass = actionClass.Trim(),
            Action = action.Trim(),
            EntityType = entityType.Trim(),
            EntityId = string.IsNullOrWhiteSpace(entityId) ? null : entityId.Trim(),
            OldValuesJson = oldValuesJson,
            NewValuesJson = newValuesJson,
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
            IpAddress = string.IsNullOrWhiteSpace(ipAddress) ? null : ipAddress.Trim(),
            TraceId = string.IsNullOrWhiteSpace(traceId) ? null : traceId.Trim(),
            RequirementCodesJson = requirementCodesJson,
            CreatedAtUtc = createdAtUtc ?? DateTime.UtcNow
        };
}
