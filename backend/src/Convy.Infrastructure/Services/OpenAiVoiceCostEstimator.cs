using Convy.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Convy.Infrastructure.Services;

public class OpenAiVoiceCostEstimator(IConfiguration configuration) : IOpenAiVoiceCostEstimator
{
    public long? EstimateMicros(VoiceParsingTelemetry telemetry)
    {
        var parsing = OpenAiParsingCost.Calculate(configuration, telemetry.InputTokens,
            telemetry.OutputTokens, telemetry.CachedTokens, telemetry.CacheWriteTokens);
        decimal total = parsing ?? 0;
        if (parsing is null && (telemetry.InputTokens is not null || telemetry.OutputTokens is not null ||
            telemetry.CachedTokens is not null || telemetry.CacheWriteTokens is not null)) return null;
        if (telemetry.AudioDurationSeconds is not null)
        {
            var price = OpenAiParsingCost.Price(configuration, "TranscriptionAudioInputMicrosPerSecond");
            if (price is null || telemetry.AudioDurationSeconds < 0) return null;
            total += (decimal)telemetry.AudioDurationSeconds.Value * price.Value;
        }
        else if (parsing is null) return null;
        return (long)Math.Round(total, MidpointRounding.AwayFromZero);
    }
}
