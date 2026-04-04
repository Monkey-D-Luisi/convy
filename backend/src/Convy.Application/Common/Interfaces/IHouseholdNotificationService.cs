using Convy.Application.Features.Items.DTOs;

namespace Convy.Application.Common.Interfaces;

public interface IHouseholdNotificationService
{
    Task NotifyItemCreated(Guid householdId, ListItemDto item, CancellationToken cancellationToken = default);
    Task NotifyItemUpdated(Guid householdId, ListItemDto item, CancellationToken cancellationToken = default);
    Task NotifyItemCompleted(Guid householdId, ListItemDto item, CancellationToken cancellationToken = default);
    Task NotifyItemUncompleted(Guid householdId, ListItemDto item, CancellationToken cancellationToken = default);
    Task NotifyItemDeleted(Guid householdId, Guid itemId, CancellationToken cancellationToken = default);
}
