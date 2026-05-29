using Convy.Domain.Common;
using Convy.Domain.ValueObjects;

namespace Convy.Domain.Entities;

public class TaskItem : Entity
{
    public string Title { get; private set; } = default!;
    public string? NormalizedTitle { get; private set; }
    public string? Note { get; private set; }
    public Guid ListId { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid? AssignedToUserId { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public DateTime? ReminderAtUtc { get; private set; }
    public DateTime? ReminderSentAtUtc { get; private set; }
    public TaskPriority Priority { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsCompleted { get; private set; }
    public Guid? CompletedBy { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private TaskItem() { }

    public TaskItem(
        string title,
        string normalizedTitle,
        Guid listId,
        Guid createdBy,
        string? note = null,
        Guid? assignedToUserId = null,
        DateOnly? dueDate = null,
        DateTime? reminderAtUtc = null,
        TaskPriority priority = TaskPriority.Normal)
        : this(title, listId, createdBy, note, assignedToUserId, dueDate, reminderAtUtc, priority)
    {
        SetNormalizedTitle(normalizedTitle);
    }

    public TaskItem(
        string title,
        Guid listId,
        Guid createdBy,
        string? note = null,
        Guid? assignedToUserId = null,
        DateOnly? dueDate = null,
        DateTime? reminderAtUtc = null,
        TaskPriority priority = TaskPriority.Normal)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Task title is required.", nameof(title));
        if (listId == Guid.Empty)
            throw new ArgumentException("List ID is required.", nameof(listId));
        if (createdBy == Guid.Empty)
            throw new ArgumentException("Creator ID is required.", nameof(createdBy));

        Title = title.Trim();
        NormalizedTitle = NormalizeBasicForComparison(Title);
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        ListId = listId;
        CreatedBy = createdBy;
        SetAssignment(assignedToUserId);
        DueDate = dueDate;
        ReminderAtUtc = NormalizeUtc(reminderAtUtc);
        ValidatePriority(priority);
        Priority = priority;
        CreatedAt = DateTime.UtcNow;
        IsCompleted = false;
    }

    public void Update(string title, string? note)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Task title is required.", nameof(title));

        Update(title, NormalizeBasicForComparison(title), note, AssignedToUserId, DueDate, ReminderAtUtc, Priority);
    }

    public void Update(
        string title,
        string? note,
        Guid? assignedToUserId,
        DateOnly? dueDate,
        DateTime? reminderAtUtc,
        TaskPriority priority)
    {
        Update(title, NormalizeBasicForComparison(title), note, assignedToUserId, dueDate, reminderAtUtc, priority);
    }

    public void Update(
        string title,
        string normalizedTitle,
        string? note,
        Guid? assignedToUserId,
        DateOnly? dueDate,
        DateTime? reminderAtUtc,
        TaskPriority priority)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Task title is required.", nameof(title));

        var normalizedReminder = NormalizeUtc(reminderAtUtc);
        var reminderChanged = ReminderAtUtc != normalizedReminder;

        Title = title.Trim();
        SetNormalizedTitle(normalizedTitle);
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        SetAssignment(assignedToUserId);
        DueDate = dueDate;
        ReminderAtUtc = normalizedReminder;
        ValidatePriority(priority);
        Priority = priority;
        if (reminderChanged)
            ReminderSentAtUtc = null;
    }

    public void Complete(Guid completedBy)
    {
        if (completedBy == Guid.Empty)
            throw new ArgumentException("Completer ID is required.", nameof(completedBy));
        if (IsCompleted)
            return;

        IsCompleted = true;
        CompletedBy = completedBy;
        CompletedAt = DateTime.UtcNow;
    }

    public void MarkReminderSent(DateTime sentAtUtc)
    {
        ReminderSentAtUtc = NormalizeUtc(sentAtUtc);
    }

    public void Uncomplete()
    {
        if (!IsCompleted)
            return;

        IsCompleted = false;
        CompletedBy = null;
        CompletedAt = null;
    }

    private void SetNormalizedTitle(string normalizedTitle)
    {
        if (string.IsNullOrWhiteSpace(normalizedTitle))
            throw new ArgumentException("Normalized task title is required.", nameof(normalizedTitle));

        NormalizedTitle = normalizedTitle.Trim();
    }

    private void SetAssignment(Guid? assignedToUserId)
    {
        if (assignedToUserId == Guid.Empty)
            throw new ArgumentException("Assignee ID must not be empty.", nameof(assignedToUserId));

        AssignedToUserId = assignedToUserId;
    }

    private static DateTime? NormalizeUtc(DateTime? value) =>
        value.HasValue ? NormalizeUtc(value.Value) : null;

    private static DateTime NormalizeUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc
            ? value
            : value.ToUniversalTime();

    private static void ValidatePriority(TaskPriority priority)
    {
        if (!Enum.IsDefined(priority))
            throw new ArgumentOutOfRangeException(nameof(priority), "Invalid task priority.");
    }

    private static string NormalizeBasicForComparison(string title) => title.Trim().ToLowerInvariant();
}
