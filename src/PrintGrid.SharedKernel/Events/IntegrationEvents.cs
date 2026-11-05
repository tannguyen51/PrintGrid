using MediatR;

namespace PrintGrid.SharedKernel.Events;

public sealed record ProductionStatusChangedIntegrationEvent(
    Guid OrderItemId,
    string NewStatus) : INotification;

public sealed record OrderDelayedIntegrationEvent(
    Guid OrderItemId) : INotification;

public sealed record OrderTimelineUpdatedIntegrationEvent(
    Guid OrderId,
    Guid CustomerId) : INotification;
