using Convy.Application.Common.Interfaces;
using Convy.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Convy.Infrastructure.Tests.Services;

public class OpenAiTaskVoiceParsingServiceTests
{
    [Fact]
    public async Task ParseAudioAsync_WhenTaskParserReportsParseError_RecordsParsingFailureTelemetry()
    {
        var transcription = new FakeTranscriptionClient
        {
            Result = new VoiceTranscriptionResult(
                "limpia la cocina",
                TimeSpan.FromSeconds(1),
                "es",
                "gpt-4o-mini-transcribe",
                null),
        };
        var parser = new FakeTaskParser
        {
            Result = new VoiceTaskParsingResult(
                [],
                new OpenAiVoiceTokenUsage(20, 5, 25, null, null, null, null),
                "gpt-5.4-nano",
                "parse_error"),
        };
        var usageRecorder = new FakeAiUsageRecorder();
        var service = new OpenAiTaskVoiceParsingService(
            transcription,
            parser,
            usageRecorder,
            new OpenAiVoiceParsingOptions("gpt-4o-mini-transcribe", "gpt-5.4-nano"),
            NullLogger<OpenAiTaskVoiceParsingService>.Instance);

        await service.ParseAudioAsync(
            new MemoryStream([1]),
            "recording.m4a",
            Guid.NewGuid(),
            [new TaskVoiceHouseholdMember(Guid.NewGuid(), "Luis")],
            "Europe/Madrid",
            DateTimeOffset.UtcNow);

        usageRecorder.Events.Should().Contain(e =>
            e.Operation == "task_parsing" &&
            e.Status == "failure" &&
            e.ErrorType == "invalid_json");
    }

    private sealed class FakeTranscriptionClient : IOpenAiVoiceTranscriptionClient
    {
        public VoiceTranscriptionResult Result { get; init; } = new("limpia la cocina", null, null, null, null);

        public Task<VoiceTranscriptionResult> TranscribeAsync(
            Stream audio,
            string fileName,
            CancellationToken cancellationToken) =>
            Task.FromResult(Result);
    }

    private sealed class FakeTaskParser : IOpenAiVoiceTaskParser
    {
        public VoiceTaskParsingResult Result { get; init; } = new([], null, null, null);

        public Task<VoiceTaskParsingResult> ParseAsync(
            string transcription,
            IReadOnlyList<TaskVoiceHouseholdMember> householdMembers,
            string timeZoneId,
            DateTimeOffset now,
            CancellationToken cancellationToken) =>
            Task.FromResult(Result);
    }

    private sealed class FakeAiUsageRecorder : IAiUsageRecorder
    {
        public List<AiUsageRecordRequest> Events { get; } = [];

        public Task RecordAsync(AiUsageRecordRequest request, CancellationToken cancellationToken = default)
        {
            Events.Add(request);
            return Task.CompletedTask;
        }
    }
}
