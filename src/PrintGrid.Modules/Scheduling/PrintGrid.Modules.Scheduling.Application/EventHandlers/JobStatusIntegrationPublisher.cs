using MediatR;
using PrintGrid.Modules.Scheduling.Domain.Events;
using PrintGrid.SharedKernel.Events;

namespace PrintGrid.Modules.Scheduling.Application.EventHandlers;

public class JobStatusIntegrationPublisher :
    INotificationHandler<JobStartedEvent>,
    INotificationHandler<JobCompletedEvent>,
    INotificationHandler<JobFailedEvent>
{
    private readonly IPublisher _publisher;

    public JobStatusIntegrationPublisher(IPublisher publisher)
    {
        _publisher = publisher;
    }

    public Task Handle(JobStartedEvent notification, CancellationToken cancellationToken)
    {
        return _publisher.Publish(
            new ProductionStatusChangedIntegrationEvent(notification.OrderItemId, "Printing"),
            cancellationToken);
    }

    public Task Handle(JobCompletedEvent notification, CancellationToken cancellationToken)
    {
        return _publisher.Publish(
            new ProductionStatusChangedIntegrationEvent(notification.OrderItemId, "QualityCheck"),
            cancellationToken);
    }

    public Task Handle(JobFailedEvent notification, CancellationToken cancellationToken)
    {
        return _publisher.Publish(
            new ProductionStatusChangedIntegrationEvent(notification.OrderItemId, "Failed", notification.Reason),
            cancellationToken);
    }
}
