using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using PrintGrid.Api.Authorization;

namespace PrintGrid.Api.Hubs;

[Authorize(Policy = Policies.RequireCustomer)]
public class OrderHub : Hub
{
    public override Task OnConnectedAsync()
    {
        var customerId = Context.User?.GetCustomerId()?.ToString();
        if (customerId != null)
        {
            Groups.AddToGroupAsync(Context.ConnectionId, CustomerGroup(customerId));
        }
        return base.OnConnectedAsync();
    }

    public static string CustomerGroup(string customerId) => $"customer:{customerId}";
}
