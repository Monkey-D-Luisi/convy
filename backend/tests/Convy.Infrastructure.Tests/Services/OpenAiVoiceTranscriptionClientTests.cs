using Convy.Infrastructure.Services;
using FluentAssertions;
using OpenAI.Audio;

namespace Convy.Infrastructure.Tests.Services;

public class OpenAiVoiceTranscriptionClientTests
{
    [Fact]
    public void CreateOptions_UsesCurrentModelCompatibleResponseFormat()
    {
        var options = OpenAiVoiceTranscriptionClient.CreateOptions();

        options.ResponseFormat.Should().Be(AudioTranscriptionFormat.Simple);
    }
}
