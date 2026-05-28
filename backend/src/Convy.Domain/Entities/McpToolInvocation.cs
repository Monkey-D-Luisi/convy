using Convy.Domain.Common;
using Convy.Domain.ValueObjects;

namespace Convy.Domain.Entities;

public class McpToolInvocation : Entity
{
    public Guid UserId { get; private set; }
    public Guid? HouseholdId { get; private set; }
    public string ToolName { get; private set; } = default!;
    public McpToolInvocationStatus Status { get; private set; }
    public long LatencyMs { get; private set; }
    public string? ErrorType { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private McpToolInvocation() { }

    public McpToolInvocation(
        Guid userId,
        Guid? householdId,
        string toolName,
        McpToolInvocationStatus status,
        long latencyMs,
        string? errorType)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID is required.", nameof(userId));
        if (householdId == Guid.Empty)
            throw new ArgumentException("Household ID must not be empty.", nameof(householdId));
        if (string.IsNullOrWhiteSpace(toolName))
            throw new ArgumentException("Tool name is required.", nameof(toolName));
        if (latencyMs < 0)
            throw new ArgumentException("Latency must not be negative.", nameof(latencyMs));

        UserId = userId;
        HouseholdId = householdId;
        ToolName = toolName.Trim();
        Status = status;
        LatencyMs = latencyMs;
        ErrorType = string.IsNullOrWhiteSpace(errorType) ? null : errorType.Trim();
        CreatedAt = DateTime.UtcNow;
    }
}
