using Convy.Domain.Common;
using Convy.Domain.ValueObjects;

namespace Convy.Domain.Entities;

public class ActivityLog : Entity
{
    public Guid HouseholdId { get; private set; }
    public ActivityEntityType EntityType { get; private set; }
    public Guid EntityId { get; private set; }
    public ActivityActionType ActionType { get; private set; }
    public Guid PerformedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public string? Metadata { get; private set; }

    private ActivityLog() { } // EF Core

    public ActivityLog(
        Guid householdId,
        ActivityEntityType entityType,
        Guid entityId,
        ActivityActionType actionType,
        Guid performedBy,
        string? metadata = null)
    {
        if (householdId == Guid.Empty)
            throw new ArgumentException("Household ID is required.", nameof(householdId));
        if (entityId == Guid.Empty)
            throw new ArgumentException("Entity ID is required.", nameof(entityId));
        if (performedBy == Guid.Empty)
            throw new ArgumentException("Performer ID is required.", nameof(performedBy));

        HouseholdId = householdId;
        EntityType = entityType;
        EntityId = entityId;
        ActionType = actionType;
        PerformedBy = performedBy;
        Metadata = metadata;
        CreatedAt = DateTime.UtcNow;
    }
}
