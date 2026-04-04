using Convy.Application.Common.Interfaces;
using Convy.Application.Features.Items.DTOs;
using Convy.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Convy.Infrastructure.Services;

public class HouseholdNotificationService : IHouseholdNotificationService
{
    private readonly IHubContext<HouseholdHub> _hubContext;

    public HouseholdNotificationService(IHubContext<HouseholdHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyItemCreated(Guid householdId, ListItemDto item, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.Group(householdId.ToString())
            .SendAsync("ItemCreated", item, cancellationToken);
    }

    public async Task NotifyItemUpdated(Guid householdId, ListItemDto item, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.Group(householdId.ToString())
            .SendAsync("ItemUpdated", item, cancellationToken);
    }

    public async Task NotifyItemCompleted(Guid householdId, ListItemDto item, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.Group(householdId.ToString())
            .SendAsync("ItemCompleted", item, cancellationToken);
    }

    public async Task NotifyItemUncompleted(Guid householdId, ListItemDto item, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.Group(householdId.ToString())
            .SendAsync("ItemUncompleted", item, cancellationToken);
    }

    public async Task NotifyItemDeleted(Guid householdId, Guid itemId, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.Group(householdId.ToString())
            .SendAsync("ItemDeleted", itemId, cancellationToken);
    }
}
