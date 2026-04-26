using Convy.Domain.Common;

namespace Convy.Domain.Entities;

public class DeviceToken : Entity
{
    public Guid UserId { get; private set; }
    public string Token { get; private set; } = default!;
    public string Platform { get; private set; } = default!;
    public string Locale { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }

    private DeviceToken() { } // EF Core

    public DeviceToken(Guid userId, string token, string platform, string? locale = null)
    {
        if (userId == Guid.Empty) throw new ArgumentException("User ID is required.", nameof(userId));
        if (string.IsNullOrWhiteSpace(token)) throw new ArgumentException("Token is required.", nameof(token));
        if (string.IsNullOrWhiteSpace(platform)) throw new ArgumentException("Platform is required.", nameof(platform));

        UserId = userId;
        Token = token;
        Platform = platform;
        Locale = NormalizeLocale(locale);
        CreatedAt = DateTime.UtcNow;
    }

    public void ReassignTo(Guid userId, string platform, string? locale = null)
    {
        if (userId == Guid.Empty) throw new ArgumentException("User ID is required.", nameof(userId));
        if (string.IsNullOrWhiteSpace(platform)) throw new ArgumentException("Platform is required.", nameof(platform));

        UserId = userId;
        Platform = platform;
        Locale = NormalizeLocale(locale);
    }

    public void UpdateLocale(string? locale)
    {
        Locale = NormalizeLocale(locale);
    }

    public static string NormalizeLocale(string? locale)
    {
        if (string.IsNullOrWhiteSpace(locale))
            return "en";

        var language = locale.Trim().Replace('_', '-').Split('-')[0].ToLowerInvariant();
        return language == "es" ? "es" : "en";
    }
}
