using Convy.Domain.Common;

namespace Convy.Domain.Entities;

public class McpOAuthRefreshToken : Entity
{
    public string TokenHash { get; private set; } = default!;
    public Guid UserId { get; private set; }
    public string ClientId { get; private set; } = default!;
    public string Resource { get; private set; } = default!;
    public string Scopes { get; private set; } = default!;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastUsedAt { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }

    private McpOAuthRefreshToken() { }

    public McpOAuthRefreshToken(
        string tokenHash,
        Guid userId,
        string clientId,
        string resource,
        string scopes,
        DateTime expiresAt)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new ArgumentException("Token hash is required.", nameof(tokenHash));
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID is required.", nameof(userId));
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("Client ID is required.", nameof(clientId));
        if (string.IsNullOrWhiteSpace(resource))
            throw new ArgumentException("Resource is required.", nameof(resource));
        if (string.IsNullOrWhiteSpace(scopes))
            throw new ArgumentException("Scopes are required.", nameof(scopes));
        if (expiresAt <= DateTime.UtcNow)
            throw new ArgumentException("Expiration must be in the future.", nameof(expiresAt));

        TokenHash = tokenHash.Trim();
        UserId = userId;
        ClientId = clientId.Trim();
        Resource = resource.Trim();
        Scopes = scopes.Trim();
        ExpiresAt = expiresAt;
        CreatedAt = DateTime.UtcNow;
    }

    public bool IsExpired(DateTime utcNow) => utcNow >= ExpiresAt;

    public void MarkUsed(DateTime utcNow)
    {
        LastUsedAt = utcNow;
    }

    public void Revoke(DateTime utcNow)
    {
        RevokedAt ??= utcNow;
    }

    public void RotateTo(string replacementTokenHash, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(replacementTokenHash))
            throw new ArgumentException("Replacement token hash is required.", nameof(replacementTokenHash));

        ReplacedByTokenHash = replacementTokenHash.Trim();
        Revoke(utcNow);
    }
}
