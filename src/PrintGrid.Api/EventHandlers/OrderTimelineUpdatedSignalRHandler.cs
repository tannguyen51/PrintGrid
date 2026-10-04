using MediatR;
using PrintGrid.Api.Hubs;
using Microsoft.AspNetCore.SignalR;
using PrintGrid.SharedKernel.Events;

namespace PrintGrid.Api.EventHandlers;

public class OrderTimelineUpdatedSignalRHandler : INotificationHandler<OrderTimelineUpdatedIntegrationEvent>
{
    private readonly IHubContext<OrderHub> _hubContext;

    public OrderTimelineUpdatedSignalRHandler(IHubContext<OrderHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task Handle(OrderTimelineUpdatedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        // Push a lightweight "invalidate" signal to this customer's group only —
        // the frontend refetches the timeline DTO (whitelist, no lab fields) on receipt.
        await _hubContext.Clients.Group(OrderHub.CustomerGroup(notification.CustomerId.ToString()))
            .SendAsync("TimelineUpdated", notification.OrderId, cancellationToken);
    }
}
