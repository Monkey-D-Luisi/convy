using Convy.Domain.Entities;
using Convy.Domain.ValueObjects;
using FluentAssertions;

namespace Convy.Domain.Tests.Entities;

public class VoiceParseEventTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesEventWithoutSensitiveContent()
    {
        var userId = Guid.NewGuid();
        var householdId = Guid.NewGuid();

        var voiceEvent = new VoiceParseEvent(
            userId,
            householdId,
            VoiceParseStatus.Success,
            audioSizeBytes: 12345,
            audioDurationSeconds: 2.5,
            parsedItemsCount: 3,
            inputTokens: 100,
            outputTokens: 20,
            cachedTokens: 70,
            reasoningTokens: 2,
            estimatedCostMicros: 42,
            latencyMs: 812);

        voiceEvent.UserId.Should().Be(userId);
        voiceEvent.HouseholdId.Should().Be(householdId);
        voiceEvent.Status.Should().Be(VoiceParseStatus.Success);
        voiceEvent.ParsedItemsCount.Should().Be(3);
        voiceEvent.EstimatedCostMicros.Should().Be(42);
        voiceEvent.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Constructor_WithNegativeParsedItems_ThrowsArgumentException()
    {
        var act = () => new VoiceParseEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            VoiceParseStatus.Success,
            audioSizeBytes: null,
            audioDurationSeconds: null,
            parsedItemsCount: -1,
            inputTokens: null,
            outputTokens: null,
            cachedTokens: null,
            reasoningTokens: null,
            estimatedCostMicros: null,
            latencyMs: 1);

        act.Should().Throw<ArgumentException>();
    }
}
