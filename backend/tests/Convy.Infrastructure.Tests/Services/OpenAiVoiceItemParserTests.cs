using Convy.Application.Features.Items.Commands;
using Convy.Infrastructure.Services;
using FluentAssertions;
using OpenAI.Responses;

namespace Convy.Infrastructure.Tests.Services;

public class OpenAiVoiceItemParserTests
{
    [Fact]
    public async Task ParseAsync_BuildsResponsesRequestAndParsesItems()
    {
        var responses = new CapturingResponsesClient("""
            {
                "items": [
                    {
                        "title": "Leche",
                        "quantity": 2,
                        "unit": "litros",
                        "matchedExistingItem": "Leche"
                    }
                ]
            }
            """);
        var parser = new OpenAiVoiceItemParser(
            responses,
            new OpenAiVoiceParsingOptions("gpt-4o-mini-transcribe", "gpt-5.4-nano"));

        var result = await parser.ParseAsync("dos litros de leche", ["Leche"], CancellationToken.None);

        result.Items.Should().Equal([new ParsedItemDto("Leche", 2, "litros", "Leche")]);
        responses.CapturedOptions.Should().NotBeNull();
        responses.CapturedOptions!.Model.Should().Be("gpt-5.4-nano");
        responses.CapturedOptions.StoredOutputEnabled.Should().BeFalse();
        responses.CapturedOptions.TextOptions!.TextFormat.Kind.Should().Be(ResponseTextFormatKind.JsonSchema);
    }

    [Fact]
    public async Task ParseAsync_PropagatesTokenUsage()
    {
        var responses = new CapturingResponsesClient("""{"items": []}""")
        {
            Usage = new OpenAiVoiceTokenUsage(
                InputTokenCount: 100,
                OutputTokenCount: 20,
                TotalTokenCount: 120,
                CachedTokenCount: 80,
                ReasoningTokenCount: 4,
                AudioTokenCount: null,
                TextTokenCount: null),
        };
        var parser = new OpenAiVoiceItemParser(
            responses,
            new OpenAiVoiceParsingOptions("gpt-4o-mini-transcribe", "gpt-5.4-nano"));

        var result = await parser.ParseAsync("nada", [], CancellationToken.None);

        result.Usage.Should().BeEquivalentTo(responses.Usage);
    }

    private sealed class CapturingResponsesClient : IOpenAiResponsesClient
    {
        private readonly string _outputText;

        public CapturingResponsesClient(string outputText)
        {
            _outputText = outputText;
        }

        public CreateResponseOptions? CapturedOptions { get; private set; }
        public OpenAiVoiceTokenUsage? Usage { get; init; }

        public Task<OpenAiResponsesResult> CreateResponseAsync(
            CreateResponseOptions options,
            CancellationToken cancellationToken)
        {
            CapturedOptions = options;
            return Task.FromResult(new OpenAiResponsesResult(_outputText, Usage, "gpt-5.4-nano", "completed"));
        }
    }
}
