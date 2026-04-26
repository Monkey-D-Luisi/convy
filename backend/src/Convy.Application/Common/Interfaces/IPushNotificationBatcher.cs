using Convy.Application.Common.Models;

namespace Convy.Application.Common.Interfaces;

public interface IPushNotificationBatcher
{
    void EnqueueNotification(
        IEnumerable<Guid> recipientUserIds,
        Guid householdId,
        Guid listId,
        string actorName,
        string listName,
        string entryTitle,
        NotificationCategory category,
        Dictionary<string, string>? data = null);
}
