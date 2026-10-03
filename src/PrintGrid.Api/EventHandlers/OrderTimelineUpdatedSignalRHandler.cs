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
        // Broadcast to the specific customer group
        // But wait! We need the latest Timeline DTO. So we should probably send a signal like "TimelineUpdated".
        // The frontend can just invalidate TanStack Query cache! Or we send the full DTO.
        // Let's send the full DTO or just an "Update" signal. The frontend will invalidate cache.
        // The prompt says: "khi có event thì invalidate/cập nhật query cache".
        // We can just send a "TimelineUpdated" event with the OrderId.
        await _hubContext.Clients.Group(OrderHub.CustomerGroup(notification.CustomerId.ToString()))
            .SendAsync("TimelineUpdated", notification.OrderId, cancellationToken);
    }
}
