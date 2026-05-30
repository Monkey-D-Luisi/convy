using Convy.Application.Common.Interfaces;
using Convy.Infrastructure.Services;
using FluentAssertions;
using OpenAI.Responses;

namespace Convy.Infrastructure.Tests.Services;

public class OpenAiVoiceTaskParserTests
{
    [Fact]
    public async Task ParseAsync_WhenProviderReturnsInvalidJson_ReturnsEmptyTasksWithParseErrorStatus()
    {
        var responses = new CapturingResponsesClient("""{"tasks":[""");
        var parser = new OpenAiVoiceTaskParser(
            responses,
            new OpenAiVoiceParsingOptions("gpt-4o-mini-transcribe", "gpt-5.4-nano"));

        var result = await parser.ParseAsync(
            "limpia la cocina",
            [new TaskVoiceHouseholdMember(Guid.NewGuid(), "Luis")],
            "Europe/Madrid",
            DateTimeOffset.UtcNow,
            CancellationToken.None);

        result.Tasks.Should().BeEmpty();
        result.Status.Should().Be("parse_error");
    }

    [Fact]
    public void Parse_WhenJsonIsInvalid_ReturnsEmptyList()
    {
        var result = OpenAiVoiceTasksResponseParser.Parse("""{"tasks":[""", 20);

        result.Should().BeEmpty();
    }

    private sealed class CapturingResponsesClient : IOpenAiResponsesClient
    {
        private readonly string _outputText;

        public CapturingResponsesClient(string outputText)
        {
            _outputText = outputText;
        }

        public Task<OpenAiResponsesResult> CreateResponseAsync(
            CreateResponseOptions options,
            CancellationToken cancellationToken) =>
            Task.FromResult(new OpenAiResponsesResult(_outputText, null, "gpt-5.4-nano", "completed"));
    }
}
