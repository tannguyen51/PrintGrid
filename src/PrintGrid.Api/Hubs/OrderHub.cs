using Microsoft.AspNetCore.SignalR;

namespace PrintGrid.Api.Hubs;

public class OrderHub : Hub
{
    public Task SubscribeToOrder(string orderId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, OrderGroup(orderId));

    public Task UnsubscribeFromOrder(string orderId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, OrderGroup(orderId));

    public static string OrderGroup(string orderId) => $"order:{orderId}";
}
