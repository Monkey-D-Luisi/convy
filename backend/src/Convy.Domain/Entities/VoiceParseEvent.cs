using Convy.Domain.Common;
using Convy.Domain.ValueObjects;

namespace Convy.Domain.Entities;

public class VoiceParseEvent : Entity
{
    public Guid UserId { get; private set; }
    public Guid HouseholdId { get; private set; }
    public VoiceParseStatus Status { get; private set; }
    public long? AudioSizeBytes { get; private set; }
    public double? AudioDurationSeconds { get; private set; }
    public int ParsedItemsCount { get; private set; }
    public int? InputTokens { get; private set; }
    public int? OutputTokens { get; private set; }
    public int? CachedTokens { get; private set; }
    public int? ReasoningTokens { get; private set; }
    public long? EstimatedCostMicros { get; private set; }
    public long LatencyMs { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private VoiceParseEvent() { }

    public VoiceParseEvent(
        Guid userId,
        Guid householdId,
        VoiceParseStatus status,
        long? audioSizeBytes,
        double? audioDurationSeconds,
        int parsedItemsCount,
        int? inputTokens,
        int? outputTokens,
        int? cachedTokens,
        int? reasoningTokens,
        long? estimatedCostMicros,
        long latencyMs)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID is required.", nameof(userId));
        if (householdId == Guid.Empty)
            throw new ArgumentException("Household ID is required.", nameof(householdId));
        if (audioSizeBytes is < 0)
            throw new ArgumentException("Audio size must not be negative.", nameof(audioSizeBytes));
        if (audioDurationSeconds is < 0)
            throw new ArgumentException("Audio duration must not be negative.", nameof(audioDurationSeconds));
        if (parsedItemsCount < 0)
            throw new ArgumentException("Parsed item count must not be negative.", nameof(parsedItemsCount));
        if (latencyMs < 0)
            throw new ArgumentException("Latency must not be negative.", nameof(latencyMs));

        UserId = userId;
        HouseholdId = householdId;
        Status = status;
        AudioSizeBytes = audioSizeBytes;
        AudioDurationSeconds = audioDurationSeconds;
        ParsedItemsCount = parsedItemsCount;
        InputTokens = inputTokens;
        OutputTokens = outputTokens;
        CachedTokens = cachedTokens;
        ReasoningTokens = reasoningTokens;
        EstimatedCostMicros = estimatedCostMicros;
        LatencyMs = latencyMs;
        CreatedAt = DateTime.UtcNow;
    }
}
