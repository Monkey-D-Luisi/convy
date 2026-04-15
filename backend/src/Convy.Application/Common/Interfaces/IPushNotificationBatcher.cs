namespace Convy.Application.Common.Interfaces;

public interface IPushNotificationBatcher
{
    void EnqueueItemNotification(
        IEnumerable<Guid> recipientUserIds,
        Guid householdId,
        Guid listId,
        string listName,
        string itemTitle,
        Dictionary<string, string>? data = null);
}
