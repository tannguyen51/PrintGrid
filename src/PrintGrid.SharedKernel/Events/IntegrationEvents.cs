using MediatR;

namespace PrintGrid.SharedKernel.Events;

public sealed record ProductionStatusChangedIntegrationEvent(
    Guid OrderItemId,
    string NewStatus,
    string? FailureReason = null) : INotification;

public sealed record OrderDelayedIntegrationEvent(
    Guid OrderItemId,
    string Reason) : INotification;

public sealed record OrderTimelineUpdatedIntegrationEvent(
    Guid OrderId,
    Guid CustomerId) : INotification;
