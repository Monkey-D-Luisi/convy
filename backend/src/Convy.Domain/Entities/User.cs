using Convy.Domain.Common;

namespace Convy.Domain.Entities;

public class User : Entity
{
    public string FirebaseUid { get; private set; } = default!;
    public string DisplayName { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }

    private readonly List<HouseholdMembership> _memberships = [];
    public IReadOnlyCollection<HouseholdMembership> Memberships => _memberships.AsReadOnly();

    private User() { } // EF Core

    public User(string firebaseUid, string displayName, string email)
    {
        if (string.IsNullOrWhiteSpace(firebaseUid))
            throw new ArgumentException("Firebase UID is required.", nameof(firebaseUid));
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Display name is required.", nameof(displayName));
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));

        FirebaseUid = firebaseUid;
        DisplayName = displayName;
        Email = email;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateProfile(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Display name is required.", nameof(displayName));

        DisplayName = displayName;
    }
}
