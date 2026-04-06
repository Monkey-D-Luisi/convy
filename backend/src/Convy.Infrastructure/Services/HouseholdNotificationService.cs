using Convy.Application.Common.Interfaces;
using Convy.Application.Features.Activity.DTOs;
using Convy.Application.Features.Items.DTOs;
using Convy.Domain.Repositories;
using Convy.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Convy.Infrastructure.Services;

public class HouseholdNotificationService : IHouseholdNotificationService
{
    private readonly IHubContext<HouseholdHub> _hubContext;
    private readonly IPushNotificationService _pushService;
    private readonly IHouseholdRepository _householdRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<HouseholdNotificationService> _logger;

    public HouseholdNotificationService(
        IHubContext<HouseholdHub> hubContext,
        IPushNotificationService pushService,
        IHouseholdRepository householdRepository,
        ICurrentUserService currentUser,
        ILogger<HouseholdNotificationService> logger)
    {
        _hubContext = hubContext;
        _pushService = pushService;
        _householdRepository = householdRepository;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task NotifyItemCreated(Guid householdId, ListItemDto item, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.Group(householdId.ToString())
            .SendAsync("ItemCreated", item, cancellationToken);
        await SendPushToOtherMembersAsync(householdId, "New item added", $"{item.Title} was added to the list", cancellationToken);
    }

    public async Task NotifyItemUpdated(Guid householdId, ListItemDto item, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.Group(householdId.ToString())
            .SendAsync("ItemUpdated", item, cancellationToken);
        await SendPushToOtherMembersAsync(householdId, "Item updated", $"{item.Title} was updated", cancellationToken);
    }

    public async Task NotifyItemCompleted(Guid householdId, ListItemDto item, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.Group(householdId.ToString())
            .SendAsync("ItemCompleted", item, cancellationToken);
        await SendPushToOtherMembersAsync(householdId, "Item completed", $"{item.Title} was marked as done", cancellationToken);
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
        await SendPushToOtherMembersAsync(householdId, "Item removed", "An item was removed from the list", cancellationToken);
    }

    public async Task NotifyListCreated(Guid householdId, Guid listId, string listName, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.Group(householdId.ToString())
            .SendAsync("ListCreated", new { listId, listName }, cancellationToken);
        await SendPushToOtherMembersAsync(householdId, "New list created", $"{listName} was created", cancellationToken);
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
        await SendPushToOtherMembersAsync(householdId, "New member", $"{displayName} joined the household", cancellationToken);
    }

    public async Task NotifyHouseholdRenamed(Guid householdId, string newName, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.Group(householdId.ToString())
            .SendAsync("HouseholdRenamed", new { householdId, newName }, cancellationToken);
    }

    public async Task NotifyMemberLeft(Guid householdId, Guid userId, string displayName, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.Group(householdId.ToString())
            .SendAsync("MemberLeft", new { userId, displayName }, cancellationToken);
        await SendPushToOtherMembersAsync(householdId, "Member left", $"{displayName} left the household", cancellationToken);
    }

    public async Task NotifyActivityLogged(Guid householdId, ActivityLogDto activity, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.Group(householdId.ToString())
            .SendAsync("ActivityLogged", activity, cancellationToken);
    }

    private async Task SendPushToOtherMembersAsync(Guid householdId, string title, string body, CancellationToken cancellationToken)
    {
        try
        {
            var household = await _householdRepository.GetByIdWithMembersAsync(householdId, cancellationToken);
            if (household is null) return;

            var otherMemberIds = household.Memberships
                .Where(m => m.UserId != _currentUser.UserId)
                .Select(m => m.UserId);

            await _pushService.SendToUsersAsync(otherMemberIds, title, body, null, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send push notification for household {HouseholdId}", householdId);
        }
    }
}
