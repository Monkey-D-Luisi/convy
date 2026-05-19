using Convy.Application.Features.Items.Commands;
using Convy.Domain.ValueObjects;

namespace Convy.Application.Common.Interfaces;

public interface IAiVoiceParsingService
{
    Task<VoiceParsingResult> ParseAudioAsync(
        Stream audio,
        string fileName,
        Guid householdId,
        CancellationToken cancellationToken = default);
}

public interface IOpenAiVoiceCostEstimator
{
    long? EstimateMicros(VoiceParsingTelemetry telemetry);
}

public interface IAiUsageRecorder
{
    Task RecordAsync(AiUsageRecordRequest request, CancellationToken cancellationToken = default);
}

public record VoiceParsingResult(
    string Transcription,
    List<ParsedItemDto> Items,
    VoiceParsingTelemetry? Telemetry = null);

public record VoiceParsingTelemetry(
    VoiceParseStatus Status,
    double? AudioDurationSeconds,
    int ParsedItemsCount,
    int? InputTokens,
    int? OutputTokens,
    int? CachedTokens,
    int? ReasoningTokens,
    long LatencyMs);

public record AiUsageRecordRequest(
    Guid? HouseholdId,
    string Feature,
    string Operation,
    string? Model,
    string Status,
    long LatencyMs,
    int? InputTokens = null,
    int? OutputTokens = null,
    int? CachedTokens = null,
    int? ReasoningTokens = null,
    int? AudioTokens = null,
    int? TextTokens = null,
    double? AudioDurationSeconds = null,
    string? ErrorType = null);
