using Convy.Domain.Common;
using Convy.Domain.ValueObjects;

namespace Convy.Domain.Entities;

public class HouseholdMembership : Entity
{
    public Guid HouseholdId { get; private set; }
    public Guid UserId { get; private set; }
    public HouseholdRole Role { get; private set; }
    public DateTime JoinedAt { get; private set; }

    private HouseholdMembership() { } // EF Core

    internal HouseholdMembership(Guid householdId, Guid userId, HouseholdRole role)
    {
        if (householdId == Guid.Empty)
            throw new ArgumentException("Household ID is required.", nameof(householdId));
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID is required.", nameof(userId));

        HouseholdId = householdId;
        UserId = userId;
        Role = role;
        JoinedAt = DateTime.UtcNow;
    }
}
