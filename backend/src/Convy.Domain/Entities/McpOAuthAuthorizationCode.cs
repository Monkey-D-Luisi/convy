using Convy.Domain.Common;

namespace Convy.Domain.Entities;

public class McpOAuthAuthorizationCode : Entity
{
    public string CodeHash { get; private set; } = default!;
    public Guid UserId { get; private set; }
    public string ClientId { get; private set; } = default!;
    public string RedirectUri { get; private set; } = default!;
    public string Resource { get; private set; } = default!;
    public string Scopes { get; private set; } = default!;
    public string CodeChallenge { get; private set; } = default!;
    public string CodeChallengeMethod { get; private set; } = default!;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? UsedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private McpOAuthAuthorizationCode() { }

    public McpOAuthAuthorizationCode(
        string codeHash,
        Guid userId,
        string clientId,
        string redirectUri,
        string resource,
        string scopes,
        string codeChallenge,
        string codeChallengeMethod,
        DateTime expiresAt)
    {
        if (string.IsNullOrWhiteSpace(codeHash))
            throw new ArgumentException("Code hash is required.", nameof(codeHash));
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID is required.", nameof(userId));
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("Client ID is required.", nameof(clientId));
        if (string.IsNullOrWhiteSpace(redirectUri))
            throw new ArgumentException("Redirect URI is required.", nameof(redirectUri));
        if (string.IsNullOrWhiteSpace(resource))
            throw new ArgumentException("Resource is required.", nameof(resource));
        if (string.IsNullOrWhiteSpace(scopes))
            throw new ArgumentException("Scopes are required.", nameof(scopes));
        if (string.IsNullOrWhiteSpace(codeChallenge))
            throw new ArgumentException("Code challenge is required.", nameof(codeChallenge));
        if (!string.Equals(codeChallengeMethod, "S256", StringComparison.Ordinal))
            throw new ArgumentException("Code challenge method must be S256.", nameof(codeChallengeMethod));
        if (expiresAt <= DateTime.UtcNow)
            throw new ArgumentException("Expiration must be in the future.", nameof(expiresAt));

        CodeHash = codeHash.Trim();
        UserId = userId;
        ClientId = clientId.Trim();
        RedirectUri = redirectUri.Trim();
        Resource = resource.Trim();
        Scopes = scopes.Trim();
        CodeChallenge = codeChallenge.Trim();
        CodeChallengeMethod = codeChallengeMethod;
        ExpiresAt = expiresAt;
        CreatedAt = DateTime.UtcNow;
    }

    public bool IsExpired(DateTime utcNow) => utcNow >= ExpiresAt;

    public void MarkUsed(DateTime utcNow)
    {
        if (UsedAt.HasValue)
            throw new InvalidOperationException("Authorization code has already been used.");

        UsedAt = utcNow;
    }
}
