using OpenAI.Responses;
using System.ClientModel.Primitives;
using System.Text.Json;

namespace Convy.Infrastructure.Services;

internal interface IOpenAiResponsesClient
{
    Task<OpenAiResponsesResult> CreateResponseAsync(
        CreateResponseOptions options,
        CancellationToken cancellationToken);
}

internal sealed record OpenAiResponsesResult(
    string OutputText,
    OpenAiVoiceTokenUsage? Usage,
    string? Model,
    string? Status);

internal sealed class OpenAiResponsesClient : IOpenAiResponsesClient
{
    private readonly ResponsesClient _responsesClient;

    public OpenAiResponsesClient(ResponsesClient responsesClient)
    {
        _responsesClient = responsesClient;
    }

    public async Task<OpenAiResponsesResult> CreateResponseAsync(
        CreateResponseOptions options,
        CancellationToken cancellationToken)
    {
        var result = await _responsesClient.CreateResponseAsync(options, cancellationToken);
        var response = result.Value;

        return new OpenAiResponsesResult(
            response.GetOutputText(),
            MapUsage(response.Usage),
            response.Model,
            response.Status?.ToString());
    }

    internal static OpenAiVoiceTokenUsage? MapUsage(ResponseTokenUsage? usage)
    {
        if (usage is null)
            return null;

        // SDK 2.10 preserves untyped provider fields. Read only the usage JSON, never content.
        using var json = JsonDocument.Parse(ModelReaderWriter.Write(usage));
        int? cacheWrite = json.RootElement.TryGetProperty("input_tokens_details", out var details) &&
            details.TryGetProperty("cache_write_tokens", out var count) && count.TryGetInt32(out var tokens)
                ? tokens : null;
        return new OpenAiVoiceTokenUsage(
            usage.InputTokenCount,
            usage.OutputTokenCount,
            usage.TotalTokenCount,
            usage.InputTokenDetails?.CachedTokenCount,
            usage.OutputTokenDetails?.ReasoningTokenCount,
            null,
            null,
            cacheWrite);
    }
}
