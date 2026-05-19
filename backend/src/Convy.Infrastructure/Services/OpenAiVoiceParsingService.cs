using System.Diagnostics;
using Convy.Application.Common.Interfaces;
using Convy.Domain.Repositories;
using Convy.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Convy.Infrastructure.Services;

internal sealed class OpenAiVoiceParsingService : IAiVoiceParsingService
{
    private const int FrequentItemsLimit = 50;

    private readonly IOpenAiVoiceTranscriptionClient _transcriptionClient;
    private readonly IOpenAiVoiceItemParser _itemParser;
    private readonly IListItemRepository _itemRepository;
    private readonly IAiUsageRecorder _usageRecorder;
    private readonly OpenAiVoiceParsingOptions _options;
    private readonly ILogger<OpenAiVoiceParsingService> _logger;

    public OpenAiVoiceParsingService(
        IOpenAiVoiceTranscriptionClient transcriptionClient,
        IOpenAiVoiceItemParser itemParser,
        IListItemRepository itemRepository,
        IAiUsageRecorder usageRecorder,
        OpenAiVoiceParsingOptions options,
        ILogger<OpenAiVoiceParsingService> logger)
    {
        _transcriptionClient = transcriptionClient;
        _itemParser = itemParser;
        _itemRepository = itemRepository;
        _usageRecorder = usageRecorder;
        _options = options;
        _logger = logger;
    }

    public async Task<VoiceParsingResult> ParseAudioAsync(
        Stream audio,
        string fileName,
        Guid householdId,
        CancellationToken cancellationToken = default)
    {
        var audioBytes = audio.CanSeek ? audio.Length : (long?)null;
        var totalStopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "OpenAI voice parsing started file={FileName} audioBytes={AudioBytes}",
            fileName,
            audioBytes);

        try
        {
            var transcriptionStopwatch = Stopwatch.StartNew();
            VoiceTranscriptionResult transcription;
            try
            {
                transcription = await _transcriptionClient.TranscribeAsync(audio, fileName, cancellationToken);
            }
            catch (Exception ex)
            {
                transcriptionStopwatch.Stop();
                await RecordUsageAsync(
                    householdId,
                    "transcription",
                    _options.TranscriptionModel,
                    "failure",
                    transcriptionStopwatch.Elapsed,
                    usage: null,
                    audioDurationSeconds: null,
                    ex.GetType().Name,
                    cancellationToken);
                throw;
            }
            transcriptionStopwatch.Stop();

            LogTranscriptionCompleted(transcription, audioBytes, transcriptionStopwatch.Elapsed);
            await RecordUsageAsync(
                householdId,
                "transcription",
                transcription.Model ?? _options.TranscriptionModel,
                "success",
                transcriptionStopwatch.Elapsed,
                transcription.Usage,
                transcription.Duration?.TotalSeconds,
                errorType: null,
                cancellationToken);

            if (string.IsNullOrWhiteSpace(transcription.Text))
            {
                LogParsingCompleted("empty_transcription", 0, null, null, null, TimeSpan.Zero);
                totalStopwatch.Stop();
                return new VoiceParsingResult(
                    string.Empty,
                    [],
                    CreateTelemetry(VoiceParseStatus.EmptyTranscription, transcription, null, 0, totalStopwatch.Elapsed));
            }

            var existingItems = await _itemRepository.GetFrequentTitlesAsync(
                householdId,
                null,
                FrequentItemsLimit,
                cancellationToken);

            var parsingStopwatch = Stopwatch.StartNew();
            VoiceItemParsingResult parsing;
            try
            {
                parsing = await _itemParser.ParseAsync(transcription.Text, existingItems, cancellationToken);
            }
            catch (Exception ex)
            {
                parsingStopwatch.Stop();
                await RecordUsageAsync(
                    householdId,
                    "parsing",
                    _options.ParsingModel,
                    "failure",
                    parsingStopwatch.Elapsed,
                    usage: null,
                    audioDurationSeconds: null,
                    ex.GetType().Name,
                    cancellationToken);
                throw;
            }
            parsingStopwatch.Stop();

            var result = parsing.Items.Count == 0 ? "parse_empty" : "success";
            LogParsingCompleted(
                result,
                parsing.Items.Count,
                parsing.Usage,
                parsing.Model,
                parsing.Status,
                parsingStopwatch.Elapsed);
            await RecordUsageAsync(
                householdId,
                "parsing",
                parsing.Model ?? _options.ParsingModel,
                "success",
                parsingStopwatch.Elapsed,
                parsing.Usage,
                audioDurationSeconds: null,
                errorType: null,
                cancellationToken);

            totalStopwatch.Stop();
            return new VoiceParsingResult(
                transcription.Text,
                parsing.Items.ToList(),
                CreateTelemetry(
                    parsing.Items.Count == 0 ? VoiceParseStatus.ParseEmpty : VoiceParseStatus.Success,
                    transcription,
                    parsing.Usage,
                    parsing.Items.Count,
                    totalStopwatch.Elapsed));
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

    private async Task RecordUsageAsync(
        Guid householdId,
        string operation,
        string? model,
        string status,
        TimeSpan elapsed,
        OpenAiVoiceTokenUsage? usage,
        double? audioDurationSeconds,
        string? errorType,
        CancellationToken cancellationToken)
    {
        var request = new AiUsageRecordRequest(
            householdId,
            "voice",
            operation,
            model,
            status,
            (long)elapsed.TotalMilliseconds,
            usage?.InputTokenCount,
            usage?.OutputTokenCount,
            usage?.CachedTokenCount,
            usage?.ReasoningTokenCount,
            usage?.AudioTokenCount,
            usage?.TextTokenCount,
            audioDurationSeconds,
            errorType);

        await _usageRecorder.RecordAsync(request, cancellationToken);
    }

    private static VoiceParsingTelemetry CreateTelemetry(
        VoiceParseStatus status,
        VoiceTranscriptionResult transcription,
        OpenAiVoiceTokenUsage? parsingUsage,
        int parsedItemsCount,
        TimeSpan totalElapsed)
    {
        return new VoiceParsingTelemetry(
            status,
            transcription.Duration?.TotalSeconds,
            parsedItemsCount,
            parsingUsage?.InputTokenCount,
            parsingUsage?.OutputTokenCount,
            parsingUsage?.CachedTokenCount,
            parsingUsage?.ReasoningTokenCount,
            (long)totalElapsed.TotalMilliseconds);
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
