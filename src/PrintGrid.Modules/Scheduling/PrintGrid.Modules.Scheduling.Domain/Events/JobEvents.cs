using PrintGrid.SharedKernel.Common;

namespace PrintGrid.Modules.Scheduling.Domain.Events;

public sealed record JobAssignedEvent(
    Guid JobId,
    Guid LabId,
    Guid MachineId,
    DateTime PlannedStartUtc,
    DateTime PlannedEndUtc,
    decimal Score) : DomainEvent;

public sealed record JobFailedEvent(
    Guid JobId,
    Guid LabId,
    Guid MachineId,
    string Reason,
    int AttemptNumber,
    Guid OrderItemId) : DomainEvent;

public sealed record JobDeclinedEvent(
    Guid JobId,
    Guid LabId,
    string Reason) : DomainEvent;

/// <summary>
/// Raised when a production event invalidates a placement and the plan must be repaired
/// (FR-SCHED-007). <paramref name="ExcludedLabId"/> carries the party responsible for the
/// event — the lab that declined must not be offered the same job straight back.
/// </summary>
public sealed record ReschedulingTriggeredEvent(
    Guid JobId,
    string Trigger,
    DateOnly InternalDueDate,
    Guid? ExcludedLabId = null) : DomainEvent;

/// <summary>
/// A reprint job was created for a failed job (FR-HUB-003 / BR-RESCHED-003): URGENT,
/// inheriting the original deadline, cost charged to the at-fault party.
/// </summary>
public sealed record JobReprintCreatedEvent(
    Guid JobId,
    Guid OriginalJobId,
    int ReprintIndex,
    Guid? FaultLabId,
    DateOnly InheritedDueDate) : DomainEvent;

public sealed record JobStartedEvent(
    Guid JobId,
    Guid OrderItemId) : DomainEvent;

public sealed record JobCompletedEvent(
    Guid JobId,
    Guid OrderItemId) : DomainEvent;

public sealed record JobInspectionPassedEvent(
    Guid JobId,
    IReadOnlyList<string> PhotoUrls) : DomainEvent;

public sealed record CustomerFaultInspectionFailedEvent(
    Guid JobId,
    string? FailureReason,
    IReadOnlyList<string> PhotoUrls,
    string CustomerNotificationMessage) : DomainEvent;

