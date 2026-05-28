using Convy.Domain.Common;

namespace Convy.Domain.Entities;

public class McpOAuthConsent : Entity
{
    public Guid UserId { get; private set; }
    public string ClientId { get; private set; } = default!;
    public string Resource { get; private set; } = default!;
    public string Scopes { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    private McpOAuthConsent() { }

    public McpOAuthConsent(Guid userId, string clientId, string resource, string scopes)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID is required.", nameof(userId));
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("Client ID is required.", nameof(clientId));
        if (string.IsNullOrWhiteSpace(resource))
            throw new ArgumentException("Resource is required.", nameof(resource));
        if (string.IsNullOrWhiteSpace(scopes))
            throw new ArgumentException("Scopes are required.", nameof(scopes));

        UserId = userId;
        ClientId = clientId.Trim();
        Resource = resource.Trim();
        Scopes = scopes.Trim();
        CreatedAt = DateTime.UtcNow;
    }

    public void Revoke(DateTime utcNow)
    {
        RevokedAt ??= utcNow;
    }
}
