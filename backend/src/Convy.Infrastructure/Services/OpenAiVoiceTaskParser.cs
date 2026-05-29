using System.Text.Json;
using Convy.Application.Common.Interfaces;
using Convy.Application.Features.Tasks.Commands;
using Convy.Domain.ValueObjects;

namespace Convy.Infrastructure.Services;

internal interface IOpenAiVoiceTaskParser
{
    Task<VoiceTaskParsingResult> ParseAsync(
        string transcription,
        IReadOnlyList<TaskVoiceHouseholdMember> householdMembers,
        string timeZoneId,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}

internal sealed record VoiceTaskParsingResult(
    IReadOnlyList<ParsedTaskDto> Tasks,
    OpenAiVoiceTokenUsage? Usage,
    string? Model,
    string? Status);

internal sealed class OpenAiVoiceTaskParser : IOpenAiVoiceTaskParser
{
    private readonly IOpenAiResponsesClient _responsesClient;
    private readonly OpenAiVoiceParsingOptions _options;

    public OpenAiVoiceTaskParser(
        IOpenAiResponsesClient responsesClient,
        OpenAiVoiceParsingOptions options)
    {
        _responsesClient = responsesClient;
        _options = options;
    }

    public async Task<VoiceTaskParsingResult> ParseAsync(
        string transcription,
        IReadOnlyList<TaskVoiceHouseholdMember> householdMembers,
        string timeZoneId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var options = OpenAiTaskVoiceParsingPromptFactory.CreateResponseOptions(
            _options.ParsingModel,
            transcription,
            householdMembers,
            timeZoneId,
            now,
            _options.MaxOutputTokenCount);

        var response = await _responsesClient.CreateResponseAsync(options, cancellationToken);
        var tasks = OpenAiVoiceTasksResponseParser.Parse(response.OutputText, _options.MaxParsedItems);

        return new VoiceTaskParsingResult(tasks, response.Usage, response.Model, response.Status);
    }
}

internal static class OpenAiVoiceTasksResponseParser
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    public static IReadOnlyList<ParsedTaskDto> Parse(string json, int maxTasks)
    {
        var parsed = JsonSerializer.Deserialize<VoiceTasksResponse>(json, JsonSerializerOptions);
        return parsed?.Tasks?
            .Where(task => !string.IsNullOrWhiteSpace(task.Title))
            .Take(maxTasks)
            .Select(task => new ParsedTaskDto(
                task.Title.Trim(),
                string.IsNullOrWhiteSpace(task.Note) ? null : task.Note.Trim(),
                ParseGuid(task.AssignedToUserId),
                ParseDate(task.DueDate),
                ParseDateTime(task.ReminderAtUtc),
                ParsePriority(task.Priority),
                string.IsNullOrWhiteSpace(task.MatchedExistingTask) ? null : task.MatchedExistingTask.Trim()))
            .ToList()
            ?? [];
    }

    private static Guid? ParseGuid(string? value) =>
        Guid.TryParse(value, out var id) ? id : null;

    private static DateOnly? ParseDate(string? value) =>
        DateOnly.TryParse(value, out var date) ? date : null;

    private static DateTime? ParseDateTime(string? value) =>
        DateTimeOffset.TryParse(value, out var instant)
            ? instant.UtcDateTime
            : null;

    private static TaskPriority ParsePriority(string? value) =>
        Enum.TryParse<TaskPriority>(value, ignoreCase: true, out var priority)
            ? priority
            : TaskPriority.Normal;

    private sealed class VoiceTasksResponse
    {
        public List<VoiceTaskEntry>? Tasks { get; set; }
    }

    private sealed class VoiceTaskEntry
    {
        public string Title { get; set; } = "";
        public string? Note { get; set; }
        public string? AssignedToUserId { get; set; }
        public string? DueDate { get; set; }
        public string? ReminderAtUtc { get; set; }
        public string? Priority { get; set; }
        public string? MatchedExistingTask { get; set; }
    }
}
