using Convy.Application.Common.Interfaces;
using Convy.Application.Common.Models;
using Convy.Application.Features.Activity.DTOs;
using Convy.Application.Features.Items.DTOs;
using Convy.Application.Features.Tasks.DTOs;
using Convy.Domain.Repositories;
using Convy.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Convy.Infrastructure.Services;

public class HouseholdNotificationService : IHouseholdNotificationService
{
    private readonly IHubContext<HouseholdHub> _hubContext;
    private readonly IPushNotificationService _pushService;
    private readonly IPushNotificationBatcher _pushBatcher;
    private readonly IHouseholdRepository _householdRepository;
    private readonly IHouseholdListRepository _listRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<HouseholdNotificationService> _logger;

    public HouseholdNotificationService(
        IHubContext<HouseholdHub> hubContext,
        IPushNotificationService pushService,
        IPushNotificationBatcher pushBatcher,
        IHouseholdRepository householdRepository,
        IHouseholdListRepository listRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUser,
        ILogger<HouseholdNotificationService> logger)
    {
        _hubContext = hubContext;
        _pushService = pushService;
        _pushBatcher = pushBatcher;
        _householdRepository = householdRepository;
        _listRepository = listRepository;
        _userRepository = userRepository;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task NotifyItemCreated(Guid householdId, ListItemDto item, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.Group(householdId.ToString())
            .SendAsync("ItemCreated", item, cancellationToken);

        try
        {
            var (otherMemberIds, actorName) = await GetOtherMembersAndActorAsync(householdId, cancellationToken);
            if (otherMemberIds.Count == 0) return;

            var list = await _listRepository.GetByIdAsync(item.ListId, cancellationToken);
            var listName = list?.Name ?? "the list";

            _pushBatcher.EnqueueNotification(
                otherMemberIds,
                householdId,
                item.ListId,
                actorName,
                listName,
                item.Title,
                NotificationCategory.ItemsAdded,
                new Dictionary<string, string>
                {
                    ["type"] = "item",
                    ["listId"] = item.ListId.ToString()
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue batched push notification for household {HouseholdId}", householdId);
        }
    }

    public async Task NotifyItemUpdated(Guid householdId, ListItemDto item, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.Group(householdId.ToString())
            .SendAsync("ItemUpdated", item, cancellationToken);
        await SendLocalizedPushToOtherMembersAsync(
            householdId,
            NotificationCategory.ItemTaskChanges,
            new PushNotificationTemplate(
                NotificationTemplateKey.ItemUpdated,
                new Dictionary<string, string> { ["title"] = item.Title }),
            cancellationToken: cancellationToken);
    }

    public async Task NotifyItemCompleted(Guid householdId, ListItemDto item, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.Group(householdId.ToString())
            .SendAsync("ItemCompleted", item, cancellationToken);
        await EnqueueCompletionNotificationAsync(
            householdId,
            item.ListId,
            item.Title,
            NotificationCategory.ItemsCompleted,
            cancellationToken);
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
        await SendLocalizedPushToOtherMembersAsync(
            householdId,
            NotificationCategory.ItemTaskChanges,
            new PushNotificationTemplate(NotificationTemplateKey.ItemDeleted, new Dictionary<string, string>()),
            cancellationToken: cancellationToken);
    }

    public async Task NotifyTaskCreated(Guid householdId, TaskItemDto task, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.Group(householdId.ToString())
            .SendAsync("TaskCreated", task, cancellationToken);
        await EnqueueCreatedNotificationAsync(
            householdId,
            task.ListId,
            task.Title,
            NotificationCategory.TasksAdded,
            "task",
            cancellationToken);
    }

    public async Task NotifyTaskUpdated(Guid householdId, TaskItemDto task, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.Group(householdId.ToString())
            .SendAsync("TaskUpdated", task, cancellationToken);
        await SendLocalizedPushToOtherMembersAsync(
            householdId,
            NotificationCategory.ItemTaskChanges,
            new PushNotificationTemplate(
                NotificationTemplateKey.TaskUpdated,
                new Dictionary<string, string> { ["title"] = task.Title }),
            cancellationToken: cancellationToken);
    }

    public async Task NotifyTaskCompleted(Guid householdId, TaskItemDto task, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.Group(householdId.ToString())
            .SendAsync("TaskCompleted", task, cancellationToken);
        await EnqueueCompletionNotificationAsync(
            householdId,
            task.ListId,
            task.Title,
            NotificationCategory.TasksCompleted,
            cancellationToken);
    }

    public async Task NotifyTaskUncompleted(Guid householdId, TaskItemDto task, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.Group(householdId.ToString())
            .SendAsync("TaskUncompleted", task, cancellationToken);
    }

    public async Task NotifyTaskDeleted(Guid householdId, Guid taskId, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.Group(householdId.ToString())
            .SendAsync("TaskDeleted", taskId, cancellationToken);
        await SendLocalizedPushToOtherMembersAsync(
            householdId,
            NotificationCategory.ItemTaskChanges,
            new PushNotificationTemplate(NotificationTemplateKey.TaskDeleted, new Dictionary<string, string>()),
            cancellationToken: cancellationToken);
    }

    public async Task NotifyListCreated(Guid householdId, Guid listId, string listName, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.Group(householdId.ToString())
            .SendAsync("ListCreated", new { listId, listName }, cancellationToken);
        await SendLocalizedPushToOtherMembersAsync(
            householdId,
            NotificationCategory.ListChanges,
            new PushNotificationTemplate(
                NotificationTemplateKey.ListCreated,
                new Dictionary<string, string> { ["listName"] = listName }),
            cancellationToken: cancellationToken);
    }

    public async Task NotifyListRenamed(Guid householdId, Guid listId, string newName, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.Group(householdId.ToString())
            .SendAsync("ListRenamed", new { listId, newName }, cancellationToken);
        await SendLocalizedPushToOtherMembersAsync(
            householdId,
            NotificationCategory.ListChanges,
            new PushNotificationTemplate(
                NotificationTemplateKey.ListRenamed,
                new Dictionary<string, string> { ["listName"] = newName }),
            cancellationToken: cancellationToken);
    }

    public async Task NotifyListArchived(Guid householdId, Guid listId, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.Group(householdId.ToString())
            .SendAsync("ListArchived", new { listId }, cancellationToken);
        var list = await _listRepository.GetByIdAsync(listId, cancellationToken);
        await SendLocalizedPushToOtherMembersAsync(
            householdId,
            NotificationCategory.ListChanges,
            new PushNotificationTemplate(
                NotificationTemplateKey.ListArchived,
                new Dictionary<string, string> { ["listName"] = list?.Name ?? "the list" }),
            cancellationToken: cancellationToken);
    }

    public async Task NotifyMemberJoined(Guid householdId, string userId, string displayName, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.Group(householdId.ToString())
            .SendAsync("MemberJoined", new { userId, displayName }, cancellationToken);
        await SendLocalizedPushToOtherMembersAsync(
            householdId,
            NotificationCategory.MemberChanges,
            new PushNotificationTemplate(
                NotificationTemplateKey.MemberJoined,
                new Dictionary<string, string> { ["displayName"] = displayName }),
            cancellationToken: cancellationToken);
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
        await SendLocalizedPushToOtherMembersAsync(
            householdId,
            NotificationCategory.MemberChanges,
            new PushNotificationTemplate(
                NotificationTemplateKey.MemberLeft,
                new Dictionary<string, string> { ["displayName"] = displayName }),
            cancellationToken: cancellationToken);
    }

    public async Task NotifyActivityLogged(Guid householdId, ActivityLogDto activity, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.Group(householdId.ToString())
            .SendAsync("ActivityLogged", activity, cancellationToken);
    }

    private async Task EnqueueCreatedNotificationAsync(
        Guid householdId,
        Guid listId,
        string title,
        NotificationCategory category,
        string type,
        CancellationToken cancellationToken)
    {
        try
        {
            var (otherMemberIds, actorName) = await GetOtherMembersAndActorAsync(householdId, cancellationToken);
            if (otherMemberIds.Count == 0) return;

            var list = await _listRepository.GetByIdAsync(listId, cancellationToken);
            var listName = list?.Name ?? "the list";

            _pushBatcher.EnqueueNotification(
                otherMemberIds,
                householdId,
                listId,
                actorName,
                listName,
                title,
                category,
                new Dictionary<string, string>
                {
                    ["type"] = type,
                    ["listId"] = listId.ToString()
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue created push notification for household {HouseholdId}", householdId);
        }
    }

    private async Task EnqueueCompletionNotificationAsync(
        Guid householdId,
        Guid listId,
        string title,
        NotificationCategory category,
        CancellationToken cancellationToken)
    {
        try
        {
            var (otherMemberIds, actorName) = await GetOtherMembersAndActorAsync(householdId, cancellationToken);
            if (otherMemberIds.Count == 0) return;

            var list = await _listRepository.GetByIdAsync(listId, cancellationToken);
            var listName = list?.Name ?? "the list";

            _pushBatcher.EnqueueNotification(
                otherMemberIds,
                householdId,
                listId,
                actorName,
                listName,
                title,
                category,
                new Dictionary<string, string>
                {
                    ["type"] = category == NotificationCategory.TasksCompleted ? "task" : "item",
                    ["listId"] = listId.ToString()
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue completion push notification for household {HouseholdId}", householdId);
        }
    }

    private async Task SendLocalizedPushToOtherMembersAsync(
        Guid householdId,
        NotificationCategory category,
        PushNotificationTemplate template,
        Dictionary<string, string>? data = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var (otherMemberIds, _) = await GetOtherMembersAndActorAsync(householdId, cancellationToken);
            if (otherMemberIds.Count == 0) return;

            await _pushService.SendLocalizedAsync(otherMemberIds, category, template, data, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send localized push notification for household {HouseholdId}", householdId);
        }
    }

    private async Task<(IReadOnlyList<Guid> OtherMemberIds, string ActorName)> GetOtherMembersAndActorAsync(
        Guid householdId,
        CancellationToken cancellationToken)
    {
        var household = await _householdRepository.GetByIdWithMembersAsync(householdId, cancellationToken);
        if (household is null) return ([], "Someone");

        var otherMemberIds = household.Memberships
            .Where(m => m.UserId != _currentUser.UserId)
            .Select(m => m.UserId)
            .ToList();

        var actor = await _userRepository.GetByIdAsync(_currentUser.UserId, cancellationToken);
        return (otherMemberIds, actor?.DisplayName ?? "Someone");
    }
}
