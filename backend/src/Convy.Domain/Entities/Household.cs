using Convy.Domain.Common;
using Convy.Domain.Exceptions;
using Convy.Domain.ValueObjects;

namespace Convy.Domain.Entities;

public class Household : Entity
{
    public string Name { get; private set; } = default!;
    public Guid CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private readonly List<HouseholdMembership> _memberships = [];
    public IReadOnlyCollection<HouseholdMembership> Memberships => _memberships.AsReadOnly();

    private Household() { } // EF Core

    public Household(string name, Guid createdBy)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Household name is required.", nameof(name));
        if (createdBy == Guid.Empty)
            throw new ArgumentException("Creator ID is required.", nameof(createdBy));

        Name = name;
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;

        _memberships.Add(new HouseholdMembership(Id, createdBy, HouseholdRole.Owner));
    }

    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Household name is required.", nameof(newName));

        Name = newName;
    }

    public HouseholdMembership AddMember(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID is required.", nameof(userId));

        if (_memberships.Any(m => m.UserId == userId))
            throw new DomainException("User is already a member of this household.");

        var membership = new HouseholdMembership(Id, userId, HouseholdRole.Member);
        _memberships.Add(membership);
        return membership;
    }

    public bool IsMember(Guid userId) => _memberships.Any(m => m.UserId == userId);
}
