using System.Text.Json;
using Convy.Application.Features.Items.Commands;

namespace Convy.Infrastructure.Services;

internal interface IOpenAiVoiceItemParser
{
    Task<VoiceItemParsingResult> ParseAsync(
        string transcription,
        IReadOnlyList<string> existingItems,
        CancellationToken cancellationToken);
}

internal sealed record VoiceItemParsingResult(
    IReadOnlyList<ParsedItemDto> Items,
    OpenAiVoiceTokenUsage? Usage,
    string? Model,
    string? Status);

internal sealed class OpenAiVoiceItemParser : IOpenAiVoiceItemParser
{
    private readonly IOpenAiResponsesClient _responsesClient;
    private readonly OpenAiVoiceParsingOptions _options;

    public OpenAiVoiceItemParser(
        IOpenAiResponsesClient responsesClient,
        OpenAiVoiceParsingOptions options)
    {
        _responsesClient = responsesClient;
        _options = options;
    }

    public async Task<VoiceItemParsingResult> ParseAsync(
        string transcription,
        IReadOnlyList<string> existingItems,
        CancellationToken cancellationToken)
    {
        var options = OpenAiVoiceParsingPromptFactory.CreateResponseOptions(
            _options.ParsingModel,
            transcription,
            existingItems,
            _options.MaxOutputTokenCount);

        var response = await _responsesClient.CreateResponseAsync(options, cancellationToken);
        var items = OpenAiVoiceItemsResponseParser.Parse(response.OutputText, _options.MaxParsedItems);

        return new VoiceItemParsingResult(items, response.Usage, response.Model, response.Status);
    }
}

internal static class OpenAiVoiceItemsResponseParser
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    public static IReadOnlyList<ParsedItemDto> Parse(string json, int maxItems)
    {
        var parsed = JsonSerializer.Deserialize<VoiceItemsResponse>(json, JsonSerializerOptions);
        return parsed?.Items?
            .Where(i => !string.IsNullOrWhiteSpace(i.Title))
            .Take(maxItems)
            .Select(i => new ParsedItemDto(
                i.Title.Trim(),
                i.Quantity,
                string.IsNullOrWhiteSpace(i.Unit) ? null : i.Unit.Trim(),
                string.IsNullOrWhiteSpace(i.MatchedExistingItem) ? null : i.MatchedExistingItem.Trim()))
            .ToList()
            ?? [];
    }

    private sealed class VoiceItemsResponse
    {
        public List<VoiceItemEntry>? Items { get; set; }
    }

    private sealed class VoiceItemEntry
    {
        public string Title { get; set; } = "";
        public int? Quantity { get; set; }
        public string? Unit { get; set; }
        public string? MatchedExistingItem { get; set; }
    }
}
