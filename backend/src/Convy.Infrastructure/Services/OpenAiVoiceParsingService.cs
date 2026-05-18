using System.Diagnostics;
using Convy.Application.Common.Interfaces;
using Convy.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Convy.Infrastructure.Services;

internal sealed class OpenAiVoiceParsingService : IAiVoiceParsingService
{
    private const int FrequentItemsLimit = 50;

    private readonly IOpenAiVoiceTranscriptionClient _transcriptionClient;
    private readonly IOpenAiVoiceItemParser _itemParser;
    private readonly IListItemRepository _itemRepository;
    private readonly ILogger<OpenAiVoiceParsingService> _logger;

    public OpenAiVoiceParsingService(
        IOpenAiVoiceTranscriptionClient transcriptionClient,
        IOpenAiVoiceItemParser itemParser,
        IListItemRepository itemRepository,
        ILogger<OpenAiVoiceParsingService> logger)
    {
        _transcriptionClient = transcriptionClient;
        _itemParser = itemParser;
        _itemRepository = itemRepository;
        _logger = logger;
    }

    public async Task<VoiceParsingResult> ParseAudioAsync(
        Stream audio,
        string fileName,
        Guid householdId,
        CancellationToken cancellationToken = default)
    {
        var audioBytes = audio.CanSeek ? audio.Length : (long?)null;

        _logger.LogInformation(
            "OpenAI voice parsing started file={FileName} audioBytes={AudioBytes}",
            fileName,
            audioBytes);

        try
        {
            var transcriptionStopwatch = Stopwatch.StartNew();
            var transcription = await _transcriptionClient.TranscribeAsync(audio, fileName, cancellationToken);
            transcriptionStopwatch.Stop();

            LogTranscriptionCompleted(transcription, audioBytes, transcriptionStopwatch.Elapsed);

            if (string.IsNullOrWhiteSpace(transcription.Text))
            {
                LogParsingCompleted("empty_transcription", 0, null, null, null, TimeSpan.Zero);
                return new VoiceParsingResult(string.Empty, []);
            }

            var existingItems = await _itemRepository.GetFrequentTitlesAsync(
                householdId,
                null,
                FrequentItemsLimit,
                cancellationToken);

            var parsingStopwatch = Stopwatch.StartNew();
            var parsing = await _itemParser.ParseAsync(transcription.Text, existingItems, cancellationToken);
            parsingStopwatch.Stop();

            var result = parsing.Items.Count == 0 ? "parse_empty" : "success";
            LogParsingCompleted(
                result,
                parsing.Items.Count,
                parsing.Usage,
                parsing.Model,
                parsing.Status,
                parsingStopwatch.Elapsed);

            return new VoiceParsingResult(transcription.Text, parsing.Items.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                "OpenAI voice parsing completed result={Result} file={FileName} audioBytes={AudioBytes} exceptionType={ExceptionType}",
                "provider_error",
                fileName,
                audioBytes,
                ex.GetType().Name);
            throw;
        }
    }

    private void LogTranscriptionCompleted(
        VoiceTranscriptionResult transcription,
        long? audioBytes,
        TimeSpan elapsed)
    {
        _logger.LogInformation(
            "OpenAI audio transcription completed result={Result} model={Model} audioBytes={AudioBytes} durationMs={DurationMs} language={Language} elapsedMs={ElapsedMs} inputTokens={InputTokens} outputTokens={OutputTokens} totalTokens={TotalTokens} audioTokens={AudioTokens} textTokens={TextTokens}",
            "success",
            transcription.Model,
            audioBytes,
            transcription.Duration?.TotalMilliseconds,
            transcription.Language,
            elapsed.TotalMilliseconds,
            transcription.Usage?.InputTokenCount,
            transcription.Usage?.OutputTokenCount,
            transcription.Usage?.TotalTokenCount,
            transcription.Usage?.AudioTokenCount,
            transcription.Usage?.TextTokenCount);
    }

    private void LogParsingCompleted(
        string result,
        int itemCount,
        OpenAiVoiceTokenUsage? usage,
        string? model,
        string? status,
        TimeSpan elapsed)
    {
        _logger.LogInformation(
            "OpenAI voice item parsing completed result={Result} model={Model} status={Status} itemCount={ItemCount} elapsedMs={ElapsedMs} inputTokens={InputTokens} outputTokens={OutputTokens} totalTokens={TotalTokens} cachedTokens={CachedTokens} reasoningTokens={ReasoningTokens}",
            result,
            model,
            status,
            itemCount,
            elapsed.TotalMilliseconds,
            usage?.InputTokenCount,
            usage?.OutputTokenCount,
            usage?.TotalTokenCount,
            usage?.CachedTokenCount,
            usage?.ReasoningTokenCount);
    }
}
