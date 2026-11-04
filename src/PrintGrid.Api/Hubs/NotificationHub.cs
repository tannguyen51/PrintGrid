using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using PrintGrid.Api.Authorization;

namespace PrintGrid.Api.Hubs;

[Authorize]
public sealed class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.GetCustomerId();
        if (userId is not null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId.Value));
        }

        await base.OnConnectedAsync();
    }

    public static string UserGroup(Guid userId) => $"user:{userId}";
}
