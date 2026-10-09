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
                new OpenAiVoiceTokenUsage(20, 5, 25, 5, 1, null, null, 10),
                "gpt-6-luna",
                "parse_error"),
        };
        var usageRecorder = new FakeAiUsageRecorder();
        var service = new OpenAiTaskVoiceParsingService(
            transcription,
            parser,
            usageRecorder,
            new OpenAiVoiceParsingOptions("gpt-4o-mini-transcribe", "gpt-6-luna"),
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
            e.ErrorType == "invalid_json" && e.OutputTokens == 5 && e.ReasoningTokens == 1 && e.CacheWriteTokens == 10);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ParseAudioAsync_WithEmptyTaskTranscription_SkipsParser(string text)
    {
        var parser = new FakeTaskParser();
        var usage = new FakeAiUsageRecorder();
        var service = new OpenAiTaskVoiceParsingService(new FakeTranscriptionClient { Result = new(text, null, null, "gpt-4o-mini-transcribe", null) },
            parser, usage, new("gpt-4o-mini-transcribe", "gpt-6-luna"), NullLogger<OpenAiTaskVoiceParsingService>.Instance);
        var result = await service.ParseAudioAsync(new MemoryStream([1]), "recording.m4a", Guid.NewGuid(), [], "Europe/Madrid", DateTimeOffset.UtcNow);
        result.Transcription.Should().BeEmpty(); result.Tasks.Should().BeEmpty();
        parser.Calls.Should().Be(0); usage.Events.Should().ContainSingle(e => e.Operation == "task_transcription");
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
        public int Calls { get; private set; }
        public VoiceTaskParsingResult Result { get; init; } = new([], null, null, null);

        public Task<VoiceTaskParsingResult> ParseAsync(
            string transcription,
            IReadOnlyList<TaskVoiceHouseholdMember> householdMembers,
            string timeZoneId,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(Result);
        }
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
