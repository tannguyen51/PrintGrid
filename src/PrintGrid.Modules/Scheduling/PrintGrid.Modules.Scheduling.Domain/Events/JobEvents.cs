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
    int AttemptNumber) : DomainEvent;

public sealed record JobDeclinedEvent(
    Guid JobId,
    Guid LabId,
    string Reason) : DomainEvent;

public sealed record ReschedulingTriggeredEvent(
    Guid JobId,
    string Trigger,
    DateOnly InternalDueDate) : DomainEvent;

public sealed record JobInspectionPassedEvent(
    Guid JobId,
    IReadOnlyList<string> PhotoUrls) : DomainEvent;

public sealed record JobInspectionFailedReprintTriggeredEvent(
    Guid OriginalJobId,
    Guid ReprintJobId,
    string FaultAttribution,
    string? FailureReason,
    IReadOnlyList<string> PhotoUrls,
    DateOnly InternalDueDate,
    bool IsUrgent) : DomainEvent;

public sealed record CustomerFaultInspectionFailedEvent(
    Guid JobId,
    string? FailureReason,
    IReadOnlyList<string> PhotoUrls,
    string CustomerNotificationMessage) : DomainEvent;

