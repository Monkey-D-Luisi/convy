using Convy.Application.Features.Items.Commands;
using FluentValidation.TestHelper;

namespace Convy.Application.Tests.Features.Items;

public class ParseVoiceAudioCommandValidatorTests
{
    private readonly ParseVoiceAudioCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidData_PassesValidation()
    {
        var command = new ParseVoiceAudioCommand(Guid.NewGuid(), new MemoryStream([1, 2, 3]), "recording.m4a");
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyListId_FailsValidation()
    {
        var command = new ParseVoiceAudioCommand(Guid.Empty, new MemoryStream([1]), "test.m4a");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ListId);
    }

    [Fact]
    public void Validate_WithNullAudio_FailsValidation()
    {
        var command = new ParseVoiceAudioCommand(Guid.NewGuid(), null!, "test.m4a");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Audio);
    }

    [Fact]
    public void Validate_WithEmptyFileName_FailsValidation()
    {
        var command = new ParseVoiceAudioCommand(Guid.NewGuid(), new MemoryStream([1]), "");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.FileName);
    }

    [Fact]
    public void Validate_WithUnsupportedFormat_FailsValidation()
    {
        var command = new ParseVoiceAudioCommand(Guid.NewGuid(), new MemoryStream([1]), "test.txt");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.FileName);
    }

    [Theory]
    [InlineData("audio.mp3")]
    [InlineData("audio.mp4")]
    [InlineData("audio.m4a")]
    [InlineData("audio.wav")]
    [InlineData("audio.webm")]
    [InlineData("audio.ogg")]
    public void Validate_WithSupportedFormats_PassesValidation(string fileName)
    {
        var command = new ParseVoiceAudioCommand(Guid.NewGuid(), new MemoryStream([1]), fileName);
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.FileName);
    }
}
