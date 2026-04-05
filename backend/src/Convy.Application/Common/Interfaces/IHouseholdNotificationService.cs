using Convy.Application.Features.Activity.DTOs;
using Convy.Application.Features.Items.DTOs;

namespace Convy.Application.Common.Interfaces;

public interface IHouseholdNotificationService
{
    Task NotifyItemCreated(Guid householdId, ListItemDto item, CancellationToken cancellationToken = default);
    Task NotifyItemUpdated(Guid householdId, ListItemDto item, CancellationToken cancellationToken = default);
    Task NotifyItemCompleted(Guid householdId, ListItemDto item, CancellationToken cancellationToken = default);
    Task NotifyItemUncompleted(Guid householdId, ListItemDto item, CancellationToken cancellationToken = default);
    Task NotifyItemDeleted(Guid householdId, Guid itemId, CancellationToken cancellationToken = default);
    Task NotifyListCreated(Guid householdId, Guid listId, string listName, CancellationToken cancellationToken = default);
    Task NotifyListRenamed(Guid householdId, Guid listId, string newName, CancellationToken cancellationToken = default);
    Task NotifyListArchived(Guid householdId, Guid listId, CancellationToken cancellationToken = default);
    Task NotifyMemberJoined(Guid householdId, string userId, string displayName, CancellationToken cancellationToken = default);
    Task NotifyActivityLogged(Guid householdId, ActivityLogDto activity, CancellationToken cancellationToken = default);
}
