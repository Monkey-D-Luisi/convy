using System.Text.Json;
using Convy.Application.Common.Interfaces;
using Convy.Application.Features.Items.Commands;
using Convy.Domain.Repositories;
using Microsoft.Extensions.Logging;
using OpenAI.Audio;
using OpenAI.Chat;

namespace Convy.Infrastructure.Services;

public class OpenAiVoiceParsingService : IAiVoiceParsingService
{
    private readonly AudioClient _audioClient;
    private readonly ChatClient _chatClient;
    private readonly IListItemRepository _itemRepository;
    private readonly ILogger<OpenAiVoiceParsingService> _logger;

    private static readonly BinaryData JsonSchema = BinaryData.FromBytes("""
        {
            "type": "object",
            "properties": {
                "items": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "title": { "type": "string", "description": "Normalized item name" },
                            "quantity": { "type": ["integer", "null"], "description": "Extracted quantity or null" },
                            "unit": { "type": ["string", "null"], "description": "Unit (kg, liters, pcs, etc.) or null" },
                            "matchedExistingItem": { "type": ["string", "null"], "description": "Exact name of matched existing item or null" }
                        },
                        "required": ["title", "quantity", "unit", "matchedExistingItem"],
                        "additionalProperties": false
                    }
                }
            },
            "required": ["items"],
            "additionalProperties": false
        }
        """u8.ToArray());

    private const string SystemPrompt = """
        You are a shopping list assistant. Parse the user's voice transcription into individual list items.

        Rules:
        1. Split compound phrases into separate items (commas, "and", "y", "e", semicolons, etc.)
        2. Extract quantities and units when mentioned (e.g. "2 liters of milk" → qty: 2, unit: "liters")
        3. Normalize item names: title case, correct obvious typos
        4. If an item closely matches one from the "Existing items" list, set matchedExistingItem to that exact name and use it as the title
        5. Support Spanish and English input naturally
        6. Ignore filler words, conversational fragments, greetings, and non-item text
        7. When unsure about quantity, set it to null rather than guessing
        """;

    public OpenAiVoiceParsingService(
        AudioClient audioClient,
        ChatClient chatClient,
        IListItemRepository itemRepository,
        ILogger<OpenAiVoiceParsingService> logger)
    {
        _audioClient = audioClient;
        _chatClient = chatClient;
        _itemRepository = itemRepository;
        _logger = logger;
    }

    public async Task<VoiceParsingResult> ParseAudioAsync(
        Stream audio,
        string fileName,
        Guid householdId,
        CancellationToken cancellationToken = default)
    {
        var transcription = await TranscribeAsync(audio, fileName, cancellationToken);
        _logger.LogDebug("Transcription result: {Text}", transcription);

        var existingItems = await _itemRepository.GetFrequentTitlesAsync(householdId, null, 50, cancellationToken);

        var items = await ParseWithLlmAsync(transcription, existingItems, cancellationToken);

        return new VoiceParsingResult(transcription, items);
    }

    private async Task<string> TranscribeAsync(Stream audio, string fileName, CancellationToken cancellationToken)
    {
        var options = new AudioTranscriptionOptions
        {
            ResponseFormat = AudioTranscriptionFormat.Text,
            Language = "es",
            Prompt = "Lista de compras, supermercado, productos",
        };

        var result = await _audioClient.TranscribeAudioAsync(audio, fileName, options, cancellationToken);
        return result.Value.Text.Trim();
    }

    private async Task<List<ParsedItemDto>> ParseWithLlmAsync(
        string transcription,
        IReadOnlyList<string> existingItems,
        CancellationToken cancellationToken)
    {
        var existingItemsList = existingItems.Count > 0
            ? string.Join(", ", existingItems)
            : "(no existing items)";

        var userMessage = $"""
            Transcription: "{transcription}"
            Existing items: {existingItemsList}
            """;

        var options = new ChatCompletionOptions
        {
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                jsonSchemaFormatName: "voice_items",
                jsonSchema: JsonSchema,
                jsonSchemaIsStrict: true),
        };

        List<ChatMessage> messages =
        [
            new SystemChatMessage(SystemPrompt),
            new UserChatMessage(userMessage),
        ];

        var completion = await _chatClient.CompleteChatAsync(messages, options, cancellationToken);
        var json = completion.Value.Content[0].Text;

        _logger.LogDebug("LLM parsing result: {Json}", json);

        var parsed = JsonSerializer.Deserialize<VoiceItemsResponse>(json, JsonSerializerOptions);
        return parsed?.Items?.Select(i => new ParsedItemDto(i.Title, i.Quantity, i.Unit, i.MatchedExistingItem)).ToList()
               ?? [];
    }

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

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
