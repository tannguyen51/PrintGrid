using Microsoft.AspNetCore.SignalR;

namespace PrintGrid.Api.Hubs;

public class LabHub : Hub
{
    public Task SubscribeToLab(string labId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, LabGroup(labId));

    public Task UnsubscribeFromLab(string labId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, LabGroup(labId));

    public static string LabGroup(string labId) => $"lab:{labId}";
}
