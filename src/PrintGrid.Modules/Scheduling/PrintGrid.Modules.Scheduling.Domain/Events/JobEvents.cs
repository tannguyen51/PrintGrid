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

public sealed record ReschedulingTriggeredEvent(
    Guid JobId,
    string Trigger,
    DateOnly InternalDueDate) : DomainEvent;

public sealed record JobStartedEvent(
    Guid JobId,
    Guid OrderItemId) : DomainEvent;

public sealed record JobCompletedEvent(
    Guid JobId,
    Guid OrderItemId) : DomainEvent;

