using Convy.Domain.Entities;

namespace Convy.Application.Common.Models;

public enum NotificationCategory
{
    ItemsAdded,
    TasksAdded,
    ItemsCompleted,
    TasksCompleted,
    ItemTaskChanges,
    ListChanges,
    MemberChanges
}

public enum NotificationTemplateKey
{
    ItemsAdded,
    TasksAdded,
    ItemsCompleted,
    TasksCompleted,
    ItemUpdated,
    TaskUpdated,
    ItemDeleted,
    TaskDeleted,
    ListCreated,
    ListRenamed,
    ListArchived,
    MemberJoined,
    MemberLeft
}

public record PushNotificationTemplate(
    NotificationTemplateKey Key,
    IReadOnlyDictionary<string, string> Parameters);

public record LocalizedPushNotification(string Title, string Body);

public static class NotificationCategoryExtensions
{
    public static NotificationPreferenceCategory ToPreferenceCategory(this NotificationCategory category) => category switch
    {
        NotificationCategory.ItemsAdded => NotificationPreferenceCategory.ItemsAdded,
        NotificationCategory.TasksAdded => NotificationPreferenceCategory.TasksAdded,
        NotificationCategory.ItemsCompleted => NotificationPreferenceCategory.ItemsCompleted,
        NotificationCategory.TasksCompleted => NotificationPreferenceCategory.TasksCompleted,
        NotificationCategory.ItemTaskChanges => NotificationPreferenceCategory.ItemTaskChanges,
        NotificationCategory.ListChanges => NotificationPreferenceCategory.ListChanges,
        NotificationCategory.MemberChanges => NotificationPreferenceCategory.MemberChanges,
        _ => NotificationPreferenceCategory.ItemTaskChanges
    };
}
