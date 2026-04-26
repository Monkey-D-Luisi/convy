using Convy.Domain.Common;

namespace Convy.Domain.Entities;

public class TaskItem : Entity
{
    public string Title { get; private set; } = default!;
    public string? Note { get; private set; }
    public Guid ListId { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsCompleted { get; private set; }
    public Guid? CompletedBy { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private TaskItem() { }

    public TaskItem(string title, Guid listId, Guid createdBy, string? note = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Task title is required.", nameof(title));
        if (listId == Guid.Empty)
            throw new ArgumentException("List ID is required.", nameof(listId));
        if (createdBy == Guid.Empty)
            throw new ArgumentException("Creator ID is required.", nameof(createdBy));

        Title = title;
        Note = note;
        ListId = listId;
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
        IsCompleted = false;
    }

    public void Update(string title, string? note)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Task title is required.", nameof(title));

        Title = title;
        Note = note;
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

    public void Uncomplete()
    {
        if (!IsCompleted)
            return;

        IsCompleted = false;
        CompletedBy = null;
        CompletedAt = null;
    }
}
