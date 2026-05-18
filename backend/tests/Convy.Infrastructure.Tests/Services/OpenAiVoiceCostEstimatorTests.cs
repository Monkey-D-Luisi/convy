using Convy.Application.Common.Interfaces;
using Convy.Domain.ValueObjects;
using Convy.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace Convy.Infrastructure.Tests.Services;

public class OpenAiVoiceCostEstimatorTests
{
    [Fact]
    public void EstimateMicros_WhenRequiredPriceIsMissing_ReturnsNull()
    {
        var estimator = new OpenAiVoiceCostEstimator(new ConfigurationBuilder().Build());
        var telemetry = new VoiceParsingTelemetry(
            VoiceParseStatus.Success,
            AudioDurationSeconds: 2,
            ParsedItemsCount: 1,
            InputTokens: 100,
            OutputTokens: 25,
            CachedTokens: 80,
            ReasoningTokens: 3,
            LatencyMs: 1200);

        var cost = estimator.EstimateMicros(telemetry);

        cost.Should().BeNull();
    }

    [Fact]
    public void EstimateMicros_WithConfiguredPrices_CalculatesTotalMicros()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OpenAI:Costs:TranscriptionAudioInputMicrosPerSecond"] = "10",
                ["OpenAI:Costs:ParsingInputMicrosPer1KTokens"] = "1000",
                ["OpenAI:Costs:ParsingCachedInputMicrosPer1KTokens"] = "100",
                ["OpenAI:Costs:ParsingOutputMicrosPer1KTokens"] = "2000",
                ["OpenAI:Costs:ParsingReasoningMicrosPer1KTokens"] = "3000",
            })
            .Build();
        var estimator = new OpenAiVoiceCostEstimator(configuration);
        var telemetry = new VoiceParsingTelemetry(
            VoiceParseStatus.Success,
            AudioDurationSeconds: 2,
            ParsedItemsCount: 1,
            InputTokens: 100,
            OutputTokens: 25,
            CachedTokens: 80,
            ReasoningTokens: 3,
            LatencyMs: 1200);

        var cost = estimator.EstimateMicros(telemetry);

        cost.Should().Be(107);
    }
}
