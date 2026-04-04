using Convy.Domain.Common;
using Convy.Domain.Exceptions;
using Convy.Domain.ValueObjects;

namespace Convy.Domain.Entities;

public class HouseholdList : Entity
{
    public string Name { get; private set; } = default!;
    public ListType Type { get; private set; }
    public Guid HouseholdId { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsArchived { get; private set; }
    public DateTime? ArchivedAt { get; private set; }

    private HouseholdList() { } // EF Core

    public HouseholdList(string name, ListType type, Guid householdId, Guid createdBy)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("List name is required.", nameof(name));
        if (householdId == Guid.Empty)
            throw new ArgumentException("Household ID is required.", nameof(householdId));
        if (createdBy == Guid.Empty)
            throw new ArgumentException("Creator ID is required.", nameof(createdBy));

        Name = name;
        Type = type;
        HouseholdId = householdId;
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
        IsArchived = false;
    }

    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("List name is required.", nameof(newName));

        Name = newName;
    }

    public void Archive()
    {
        if (IsArchived)
            throw new DomainException("List is already archived.");

        IsArchived = true;
        ArchivedAt = DateTime.UtcNow;
    }
}
