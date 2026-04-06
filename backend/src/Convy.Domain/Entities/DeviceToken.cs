using Convy.Domain.Common;

namespace Convy.Domain.Entities;

public class DeviceToken : Entity
{
    public Guid UserId { get; private set; }
    public string Token { get; private set; } = default!;
    public string Platform { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }

    private DeviceToken() { } // EF Core

    public DeviceToken(Guid userId, string token, string platform)
    {
        if (userId == Guid.Empty) throw new ArgumentException("User ID is required.", nameof(userId));
        if (string.IsNullOrWhiteSpace(token)) throw new ArgumentException("Token is required.", nameof(token));
        if (string.IsNullOrWhiteSpace(platform)) throw new ArgumentException("Platform is required.", nameof(platform));

        UserId = userId;
        Token = token;
        Platform = platform;
        CreatedAt = DateTime.UtcNow;
    }
}
