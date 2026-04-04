using Convy.Domain.Common;
using Convy.Domain.Exceptions;

namespace Convy.Domain.Entities;

public class Invite : Entity
{
    public Guid HouseholdId { get; private set; }
    public string Code { get; private set; } = default!;
    public Guid CreatedBy { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? UsedAt { get; private set; }
    public Guid? UsedBy { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Invite() { } // EF Core

    public Invite(Guid householdId, Guid createdBy, TimeSpan validity)
    {
        if (householdId == Guid.Empty)
            throw new ArgumentException("Household ID is required.", nameof(householdId));
        if (createdBy == Guid.Empty)
            throw new ArgumentException("Creator ID is required.", nameof(createdBy));
        if (validity <= TimeSpan.Zero)
            throw new ArgumentException("Validity must be positive.", nameof(validity));

        HouseholdId = householdId;
        Code = GenerateCode();
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
        ExpiresAt = CreatedAt.Add(validity);
    }

    public bool IsValid => UsedAt is null && RevokedAt is null && ExpiresAt > DateTime.UtcNow;

    public void Use(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID is required.", nameof(userId));

        if (!IsValid)
            throw new DomainException("Invite is no longer valid.");

        UsedAt = DateTime.UtcNow;
        UsedBy = userId;
    }

    public void Revoke()
    {
        if (RevokedAt is not null)
            throw new DomainException("Invite is already revoked.");

        RevokedAt = DateTime.UtcNow;
    }

    private static string GenerateCode()
    {
        return Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            .Replace("/", "_")
            .Replace("+", "-")
            .TrimEnd('=')[..8]
            .ToUpperInvariant();
    }
}
