using Convy.Domain.Common;
using Convy.Domain.ValueObjects;

namespace Convy.Domain.Entities;

public class AiUsageEvent : Entity
{
    public Guid? HouseholdId { get; private set; }
    public string Feature { get; private set; } = default!;
    public string Operation { get; private set; } = default!;
    public string? Model { get; private set; }
    public AiUsageStatus Status { get; private set; }
    public string? ErrorType { get; private set; }
    public long LatencyMs { get; private set; }
    public int? InputTokens { get; private set; }
    public int? OutputTokens { get; private set; }
    public int? CachedTokens { get; private set; }
    public int? ReasoningTokens { get; private set; }
    public int? AudioTokens { get; private set; }
    public int? TextTokens { get; private set; }
    public double? AudioDurationSeconds { get; private set; }
    public long? EstimatedCostMicros { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private AiUsageEvent() { }

    public AiUsageEvent(
        Guid? householdId,
        string feature,
        string operation,
        string? model,
        AiUsageStatus status,
        long latencyMs,
        int? inputTokens,
        int? outputTokens,
        int? cachedTokens,
        int? reasoningTokens,
        int? audioTokens,
        int? textTokens,
        double? audioDurationSeconds,
        long? estimatedCostMicros,
        string? errorType = null)
    {
        if (string.IsNullOrWhiteSpace(feature))
            throw new ArgumentException("Feature is required.", nameof(feature));
        if (string.IsNullOrWhiteSpace(operation))
            throw new ArgumentException("Operation is required.", nameof(operation));
        if (latencyMs < 0)
            throw new ArgumentException("Latency must not be negative.", nameof(latencyMs));
        if (audioDurationSeconds is < 0)
            throw new ArgumentException("Audio duration must not be negative.", nameof(audioDurationSeconds));

        HouseholdId = householdId;
        Feature = feature.Trim();
        Operation = operation.Trim();
        Model = string.IsNullOrWhiteSpace(model) ? null : model.Trim();
        Status = status;
        LatencyMs = latencyMs;
        InputTokens = inputTokens;
        OutputTokens = outputTokens;
        CachedTokens = cachedTokens;
        ReasoningTokens = reasoningTokens;
        AudioTokens = audioTokens;
        TextTokens = textTokens;
        AudioDurationSeconds = audioDurationSeconds;
        EstimatedCostMicros = estimatedCostMicros;
        ErrorType = string.IsNullOrWhiteSpace(errorType) ? null : errorType.Trim();
        CreatedAt = DateTime.UtcNow;
    }
}
