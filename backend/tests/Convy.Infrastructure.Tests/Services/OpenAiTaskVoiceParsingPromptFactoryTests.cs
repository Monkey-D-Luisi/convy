using System.Text.Json;
using Convy.Infrastructure.Services;
using FluentAssertions;

namespace Convy.Infrastructure.Tests.Services;

public class OpenAiTaskVoiceParsingPromptFactoryTests
{
    [Fact]
    public void JsonSchema_LimitsTaskTitlesToShortTaskContract()
    {
        using var document = JsonDocument.Parse(OpenAiTaskVoiceParsingPromptFactory.JsonSchema);

        var taskProperties = document.RootElement
            .GetProperty("properties")
            .GetProperty("tasks")
            .GetProperty("items")
            .GetProperty("properties");

        taskProperties.GetProperty("title").GetProperty("maxLength").GetInt32().Should().Be(80);
        taskProperties.GetProperty("note").GetProperty("maxLength").GetInt32().Should().Be(500);
    }

    [Fact]
    public void SystemPrompt_DirectsLongTaskContextIntoNote()
    {
        OpenAiTaskVoiceParsingPromptFactory.SystemPrompt.Should().Contain("concise actionable title");
        OpenAiTaskVoiceParsingPromptFactory.SystemPrompt.Should().Contain("Put extra context in note");
        OpenAiTaskVoiceParsingPromptFactory.SystemPrompt.Should().Contain("Standalone issue descriptions");
    }
}
