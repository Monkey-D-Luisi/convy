using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Convy.Infrastructure.Hubs;

[Authorize]
public class HouseholdHub : Hub
{
    public async Task JoinHousehold(Guid householdId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, householdId.ToString());
    }

    public async Task LeaveHousehold(Guid householdId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, householdId.ToString());
    }
}
