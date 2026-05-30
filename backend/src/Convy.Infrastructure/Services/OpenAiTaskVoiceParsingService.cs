using System.Diagnostics;
using Convy.Application.Common.Interfaces;
using Convy.Application.Features.Tasks.Commands;
using Microsoft.Extensions.Logging;

namespace Convy.Infrastructure.Services;

internal sealed class OpenAiTaskVoiceParsingService : ITaskVoiceParsingService
{
    private readonly IOpenAiVoiceTranscriptionClient _transcriptionClient;
    private readonly IOpenAiVoiceTaskParser _taskParser;
    private readonly IAiUsageRecorder _usageRecorder;
    private readonly OpenAiVoiceParsingOptions _options;
    private readonly ILogger<OpenAiTaskVoiceParsingService> _logger;

    public OpenAiTaskVoiceParsingService(
        IOpenAiVoiceTranscriptionClient transcriptionClient,
        IOpenAiVoiceTaskParser taskParser,
        IAiUsageRecorder usageRecorder,
        OpenAiVoiceParsingOptions options,
        ILogger<OpenAiTaskVoiceParsingService> logger)
    {
        _transcriptionClient = transcriptionClient;
        _taskParser = taskParser;
        _usageRecorder = usageRecorder;
        _options = options;
        _logger = logger;
    }

    public async Task<TaskVoiceParsingResult> ParseAudioAsync(
        Stream audio,
        string fileName,
        Guid householdId,
        IReadOnlyList<TaskVoiceHouseholdMember> householdMembers,
        string timeZoneId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var transcriptionStopwatch = Stopwatch.StartNew();
        var transcription = await _transcriptionClient.TranscribeAsync(audio, fileName, cancellationToken);
        transcriptionStopwatch.Stop();
        await RecordUsageAsync(
            householdId,
            "task_transcription",
            transcription.Model ?? _options.TranscriptionModel,
            "success",
            transcriptionStopwatch.Elapsed,
            transcription.Usage,
            transcription.Duration?.TotalSeconds,
            null,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(transcription.Text))
            return new TaskVoiceParsingResult(string.Empty, []);

        var parsingStopwatch = Stopwatch.StartNew();
        var parsed = await _taskParser.ParseAsync(
            transcription.Text,
            householdMembers,
            timeZoneId,
            now,
            cancellationToken);
        parsingStopwatch.Stop();
        var parseFailed = parsed.Status == "parse_error";

        await RecordUsageAsync(
            householdId,
            "task_parsing",
            parsed.Model ?? _options.ParsingModel,
            parseFailed ? "failure" : "success",
            parsingStopwatch.Elapsed,
            parsed.Usage,
            null,
            parseFailed ? "invalid_json" : null,
            cancellationToken);

        _logger.LogInformation(
            "OpenAI voice task parsing completed result={Result} taskCount={TaskCount} model={Model}",
            parseFailed ? "parse_error" : parsed.Tasks.Count == 0 ? "parse_empty" : "success",
            parsed.Tasks.Count,
            parsed.Model);

        return new TaskVoiceParsingResult(transcription.Text, parsed.Tasks);
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
        await _usageRecorder.RecordAsync(
            new AiUsageRecordRequest(
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
                errorType),
            cancellationToken);
    }
}
