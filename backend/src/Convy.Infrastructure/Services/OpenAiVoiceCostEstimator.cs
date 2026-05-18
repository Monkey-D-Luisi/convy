using Convy.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Convy.Infrastructure.Services;

public class OpenAiVoiceCostEstimator : IOpenAiVoiceCostEstimator
{
    private readonly IConfiguration _configuration;

    public OpenAiVoiceCostEstimator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public long? EstimateMicros(VoiceParsingTelemetry telemetry)
    {
        decimal total = 0;

        if (telemetry.AudioDurationSeconds is not null)
        {
            var price = GetPrice("TranscriptionAudioInputMicrosPerSecond");
            if (price is null)
                return null;

            total += (decimal)telemetry.AudioDurationSeconds.Value * price.Value;
        }

        var cachedTokens = telemetry.CachedTokens ?? 0;
        var inputTokens = telemetry.InputTokens ?? 0;
        var nonCachedInputTokens = Math.Max(0, inputTokens - cachedTokens);

        if (nonCachedInputTokens > 0)
        {
            var price = GetPrice("ParsingInputMicrosPer1KTokens");
            if (price is null)
                return null;

            total += nonCachedInputTokens / 1000m * price.Value;
        }

        if (cachedTokens > 0)
        {
            var price = GetPrice("ParsingCachedInputMicrosPer1KTokens");
            if (price is null)
                return null;

            total += cachedTokens / 1000m * price.Value;
        }

        if (telemetry.OutputTokens is > 0)
        {
            var price = GetPrice("ParsingOutputMicrosPer1KTokens");
            if (price is null)
                return null;

            total += telemetry.OutputTokens.Value / 1000m * price.Value;
        }

        if (telemetry.ReasoningTokens is > 0)
        {
            var price = GetPrice("ParsingReasoningMicrosPer1KTokens");
            if (price is null)
                return null;

            total += telemetry.ReasoningTokens.Value / 1000m * price.Value;
        }

        return (long)Math.Round(total, MidpointRounding.AwayFromZero);
    }

    private decimal? GetPrice(string key)
    {
        var value = _configuration[$"OpenAI:Costs:{key}"];
        return decimal.TryParse(value, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }
}
