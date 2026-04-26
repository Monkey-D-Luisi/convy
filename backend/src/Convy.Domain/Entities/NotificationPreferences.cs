using Convy.Domain.Common;

namespace Convy.Domain.Entities;

public class NotificationPreferences : Entity
{
    public Guid UserId { get; private set; }
    public bool ItemsAdded { get; private set; }
    public bool TasksAdded { get; private set; }
    public bool ItemsCompleted { get; private set; }
    public bool TasksCompleted { get; private set; }
    public bool ItemTaskChanges { get; private set; }
    public bool ListChanges { get; private set; }
    public bool MemberChanges { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private NotificationPreferences() { } // EF Core

    private NotificationPreferences(Guid userId)
    {
        if (userId == Guid.Empty) throw new ArgumentException("User ID is required.", nameof(userId));

        UserId = userId;
        ItemsAdded = true;
        TasksAdded = true;
        ItemsCompleted = false;
        TasksCompleted = false;
        ItemTaskChanges = false;
        ListChanges = true;
        MemberChanges = true;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public static NotificationPreferences CreateDefault(Guid userId) => new(userId);

    public void Update(
        bool itemsAdded,
        bool tasksAdded,
        bool itemsCompleted,
        bool tasksCompleted,
        bool itemTaskChanges,
        bool listChanges,
        bool memberChanges)
    {
        ItemsAdded = itemsAdded;
        TasksAdded = tasksAdded;
        ItemsCompleted = itemsCompleted;
        TasksCompleted = tasksCompleted;
        ItemTaskChanges = itemTaskChanges;
        ListChanges = listChanges;
        MemberChanges = memberChanges;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsEnabled(NotificationPreferenceCategory category) => category switch
    {
        NotificationPreferenceCategory.ItemsAdded => ItemsAdded,
        NotificationPreferenceCategory.TasksAdded => TasksAdded,
        NotificationPreferenceCategory.ItemsCompleted => ItemsCompleted,
        NotificationPreferenceCategory.TasksCompleted => TasksCompleted,
        NotificationPreferenceCategory.ItemTaskChanges => ItemTaskChanges,
        NotificationPreferenceCategory.ListChanges => ListChanges,
        NotificationPreferenceCategory.MemberChanges => MemberChanges,
        _ => false
    };
}

public enum NotificationPreferenceCategory
{
    ItemsAdded,
    TasksAdded,
    ItemsCompleted,
    TasksCompleted,
    ItemTaskChanges,
    ListChanges,
    MemberChanges
}
