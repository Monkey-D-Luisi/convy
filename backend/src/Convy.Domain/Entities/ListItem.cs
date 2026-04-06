using Convy.Domain.Common;
using Convy.Domain.Exceptions;
using Convy.Domain.ValueObjects;

namespace Convy.Domain.Entities;

public class ListItem : Entity
{
    public string Title { get; private set; } = default!;
    public int? Quantity { get; private set; }
    public string? Unit { get; private set; }
    public string? Note { get; private set; }
    public Guid ListId { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsCompleted { get; private set; }
    public Guid? CompletedBy { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public RecurrenceFrequency? RecurrenceFrequency { get; private set; }
    public int? RecurrenceInterval { get; private set; }
    public DateTime? NextDueDate { get; private set; }

    private ListItem() { } // EF Core

    public ListItem(string title, Guid listId, Guid createdBy, int? quantity = null, string? unit = null, string? note = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Item title is required.", nameof(title));
        if (listId == Guid.Empty)
            throw new ArgumentException("List ID is required.", nameof(listId));
        if (createdBy == Guid.Empty)
            throw new ArgumentException("Creator ID is required.", nameof(createdBy));
        if (quantity.HasValue && quantity.Value <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));

        Title = title;
        ListId = listId;
        CreatedBy = createdBy;
        Quantity = quantity;
        Unit = unit;
        Note = note;
        CreatedAt = DateTime.UtcNow;
        IsCompleted = false;
    }

    public void Update(string title, int? quantity, string? unit, string? note)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Item title is required.", nameof(title));
        if (quantity.HasValue && quantity.Value <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));

        Title = title;
        Quantity = quantity;
        Unit = unit;
        Note = note;
    }

    public void Complete(Guid completedBy)
    {
        if (completedBy == Guid.Empty)
            throw new ArgumentException("Completer ID is required.", nameof(completedBy));
        if (IsCompleted)
            throw new DomainException("Item is already completed.");

        IsCompleted = true;
        CompletedBy = completedBy;
        CompletedAt = DateTime.UtcNow;
    }

    public void Uncomplete()
    {
        if (!IsCompleted)
            throw new DomainException("Item is not completed.");

        IsCompleted = false;
        CompletedBy = null;
        CompletedAt = null;
    }

    public void SetRecurrence(RecurrenceFrequency frequency, int interval)
    {
        if (interval <= 0)
            throw new ArgumentException("Recurrence interval must be greater than zero.", nameof(interval));

        RecurrenceFrequency = frequency;
        RecurrenceInterval = interval;
        NextDueDate = CalculateNextDueDate(DateTime.UtcNow, frequency, interval);
    }

    public void ClearRecurrence()
    {
        RecurrenceFrequency = null;
        RecurrenceInterval = null;
        NextDueDate = null;
    }

    public void AdvanceRecurrence()
    {
        if (RecurrenceFrequency is null || RecurrenceInterval is null)
            throw new DomainException("Item does not have a recurrence rule.");

        NextDueDate = CalculateNextDueDate(DateTime.UtcNow, RecurrenceFrequency.Value, RecurrenceInterval.Value);
    }

    private static DateTime CalculateNextDueDate(DateTime from, RecurrenceFrequency frequency, int interval)
    {
        return frequency switch
        {
            ValueObjects.RecurrenceFrequency.Daily => from.AddDays(interval),
            ValueObjects.RecurrenceFrequency.Weekly => from.AddDays(7 * interval),
            ValueObjects.RecurrenceFrequency.Monthly => from.AddMonths(interval),
            _ => throw new ArgumentOutOfRangeException(nameof(frequency)),
        };
    }
}
