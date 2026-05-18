using Convy.Application.Features.Items.Commands;
using Convy.Domain.Repositories;
using Convy.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Convy.Infrastructure.Tests.Services;

public class OpenAiVoiceParsingServiceTests
{
    private readonly IListItemRepository _itemRepository = Substitute.For<IListItemRepository>();
    private readonly CapturingLogger<OpenAiVoiceParsingService> _logger = new();
    private readonly FakeTranscriptionClient _transcription = new();
    private readonly FakeItemParser _parser = new();

    public OpenAiVoiceParsingServiceTests()
    {
        _itemRepository.GetFrequentTitlesAsync(Arg.Any<Guid>(), null, 50, Arg.Any<CancellationToken>())
            .Returns(["Leche"]);
    }

    [Fact]
    public async Task ParseAudioAsync_WithBlankTranscription_SkipsParsingAndLogsNoTranscriptText()
    {
        _transcription.Result = new VoiceTranscriptionResult(
            "",
            TimeSpan.FromSeconds(1),
            "es",
            "gpt-4o-mini-transcribe",
            new OpenAiVoiceTokenUsage(10, 0, 10, null, null, 8, 2));
        var service = CreateService();

        var result = await service.ParseAudioAsync(new MemoryStream([1, 2, 3]), "recording.m4a", Guid.NewGuid());

        result.Transcription.Should().BeEmpty();
        result.Items.Should().BeEmpty();
        _parser.CallCount.Should().Be(0);
        _logger.StructuredValues.Should().NotContain(pair => pair.Key.Contains("Transcription", StringComparison.OrdinalIgnoreCase));
        _logger.Messages.Should().Contain(message => message.Contains("empty_transcription"));
    }

    [Fact]
    public async Task ParseAudioAsync_WithParsedItems_LogsUsageWithoutSensitiveText()
    {
        const string transcript = "compra leche y pan";
        _transcription.Result = new VoiceTranscriptionResult(
            transcript,
            TimeSpan.FromSeconds(2),
            "es",
            "gpt-4o-mini-transcribe",
            new OpenAiVoiceTokenUsage(40, 0, 40, null, null, 35, 5));
        _parser.Result = new VoiceItemParsingResult(
            [new ParsedItemDto("Leche", null, null, "Leche")],
            new OpenAiVoiceTokenUsage(100, 25, 125, 80, 3, null, null),
            "gpt-5.4-nano",
            "completed");
        var service = CreateService();

        var result = await service.ParseAudioAsync(new MemoryStream([1, 2, 3]), "recording.m4a", Guid.NewGuid());

        result.Items.Should().ContainSingle(i => i.Title == "Leche");
        result.Telemetry.Should().NotBeNull();
        result.Telemetry!.Status.Should().Be(Convy.Domain.ValueObjects.VoiceParseStatus.Success);
        result.Telemetry.ParsedItemsCount.Should().Be(1);
        result.Telemetry.InputTokens.Should().Be(100);
        result.Telemetry.OutputTokens.Should().Be(25);
        result.Telemetry.CachedTokens.Should().Be(80);
        result.Telemetry.ReasoningTokens.Should().Be(3);
        result.Telemetry.AudioDurationSeconds.Should().Be(2);
        _parser.CallCount.Should().Be(1);
        _logger.ContainsValue(transcript).Should().BeFalse();
        _logger.Messages.Should().Contain(message => message.Contains("success"));
        _logger.StructuredValues.Should().Contain(pair => pair.Key == "Model" && Equals(pair.Value, "gpt-4o-mini-transcribe"));
        _logger.StructuredValues.Should().Contain(pair => pair.Key == "CachedTokens" && Equals(pair.Value, 80));
        _logger.StructuredValues.Should().Contain(pair => pair.Key == "ReasoningTokens" && Equals(pair.Value, 3));
    }

    [Fact]
    public async Task ParseAudioAsync_WhenProviderFails_LogsProviderErrorWithoutTranscriptText()
    {
        const string transcript = "compra leche";
        _transcription.Result = new VoiceTranscriptionResult(transcript, null, null, "gpt-4o-mini-transcribe", null);
        _parser.Exception = new InvalidOperationException("provider unavailable");
        var service = CreateService();

        var act = async () => await service.ParseAudioAsync(new MemoryStream([1]), "recording.m4a", Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>();
        _logger.ContainsValue(transcript).Should().BeFalse();
        _logger.Messages.Should().Contain(message => message.Contains("provider_error"));
    }

    private OpenAiVoiceParsingService CreateService() =>
        new(_transcription, _parser, _itemRepository, _logger);

    private sealed class FakeTranscriptionClient : IOpenAiVoiceTranscriptionClient
    {
        public VoiceTranscriptionResult Result { get; set; } = new("leche", null, null, null, null);

        public Task<VoiceTranscriptionResult> TranscribeAsync(
            Stream audio,
            string fileName,
            CancellationToken cancellationToken) =>
            Task.FromResult(Result);
    }

    private sealed class FakeItemParser : IOpenAiVoiceItemParser
    {
        public int CallCount { get; private set; }
        public Exception? Exception { get; set; }
        public VoiceItemParsingResult Result { get; set; } = new([], null, null, null);

        public Task<VoiceItemParsingResult> ParseAsync(
            string transcription,
            IReadOnlyList<string> existingItems,
            CancellationToken cancellationToken)
        {
            CallCount++;
            if (Exception is not null)
                throw Exception;

            return Task.FromResult(Result);
        }
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];
        public List<KeyValuePair<string, object?>> StructuredValues { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
            if (state is IEnumerable<KeyValuePair<string, object?>> values)
                StructuredValues.AddRange(values);
        }

        public bool ContainsValue(string value) =>
            Messages.Any(message => message.Contains(value, StringComparison.Ordinal))
            || StructuredValues.Any(pair => pair.Value?.ToString() == value);
    }
}
