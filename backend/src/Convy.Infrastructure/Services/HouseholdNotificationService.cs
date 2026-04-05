using Convy.Application.Common.Interfaces;
using Convy.Application.Features.Activity.DTOs;
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

    public async Task NotifyListCreated(Guid householdId, Guid listId, string listName, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.Group(householdId.ToString())
            .SendAsync("ListCreated", new { listId, listName }, cancellationToken);
    }

    public async Task NotifyListRenamed(Guid householdId, Guid listId, string newName, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.Group(householdId.ToString())
            .SendAsync("ListRenamed", new { listId, newName }, cancellationToken);
    }

    public async Task NotifyListArchived(Guid householdId, Guid listId, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.Group(householdId.ToString())
            .SendAsync("ListArchived", new { listId }, cancellationToken);
    }

    public async Task NotifyMemberJoined(Guid householdId, string userId, string displayName, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.Group(householdId.ToString())
            .SendAsync("MemberJoined", new { userId, displayName }, cancellationToken);
    }

    public async Task NotifyActivityLogged(Guid householdId, ActivityLogDto activity, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.Group(householdId.ToString())
            .SendAsync("ActivityLogged", activity, cancellationToken);
    }
}
