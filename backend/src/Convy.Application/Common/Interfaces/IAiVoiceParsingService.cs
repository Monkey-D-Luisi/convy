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
