using Convy.Domain.Common;

namespace Convy.Domain.Entities;

public class McpIdempotencyRecord : Entity
{
    public Guid UserId { get; private set; }
    public string ClientId { get; private set; } = default!;
    public string KeyHash { get; private set; } = default!;
    public string ActionName { get; private set; } = default!;
    public string RequestHash { get; private set; } = default!;
    public int StatusCode { get; private set; }
    public string? Location { get; private set; }
    public string? ResponseJson { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    private McpIdempotencyRecord() { }

    public McpIdempotencyRecord(
        Guid userId,
        string clientId,
        string keyHash,
        string actionName,
        string requestHash,
        int statusCode,
        string? location,
        string? responseJson,
        DateTime createdAt,
        DateTime expiresAt)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID is required.", nameof(userId));
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("Client ID is required.", nameof(clientId));
        if (string.IsNullOrWhiteSpace(keyHash))
            throw new ArgumentException("Key hash is required.", nameof(keyHash));
        if (string.IsNullOrWhiteSpace(actionName))
            throw new ArgumentException("Action name is required.", nameof(actionName));
        if (string.IsNullOrWhiteSpace(requestHash))
            throw new ArgumentException("Request hash is required.", nameof(requestHash));
        if (statusCode < 100 || statusCode > 599)
            throw new ArgumentOutOfRangeException(nameof(statusCode), "Status code must be an HTTP status code.");
        if (expiresAt <= createdAt)
            throw new ArgumentException("Expiry must be after creation.", nameof(expiresAt));

        UserId = userId;
        ClientId = clientId.Trim();
        KeyHash = keyHash.Trim();
        ActionName = actionName.Trim();
        RequestHash = requestHash.Trim();
        StatusCode = statusCode;
        Location = string.IsNullOrWhiteSpace(location) ? null : location.Trim();
        ResponseJson = string.IsNullOrWhiteSpace(responseJson) ? null : responseJson;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
    }

    public bool IsExpired(DateTime now) => ExpiresAt <= now;
}
