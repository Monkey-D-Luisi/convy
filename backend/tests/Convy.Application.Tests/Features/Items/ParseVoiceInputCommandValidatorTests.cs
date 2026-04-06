using Convy.Application.Features.Items.Commands;
using FluentValidation.TestHelper;

namespace Convy.Application.Tests.Features.Items;

public class ParseVoiceInputCommandValidatorTests
{
    private readonly ParseVoiceInputCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidData_PassesValidation()
    {
        var command = new ParseVoiceInputCommand(Guid.NewGuid(), "milk and bread");
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyListId_FailsValidation()
    {
        var command = new ParseVoiceInputCommand(Guid.Empty, "milk");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ListId);
    }

    [Fact]
    public void Validate_WithEmptyText_FailsValidation()
    {
        var command = new ParseVoiceInputCommand(Guid.NewGuid(), "");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.TranscribedText);
    }

    [Fact]
    public void Validate_WithLongText_FailsValidation()
    {
        var command = new ParseVoiceInputCommand(Guid.NewGuid(), new string('a', 2001));
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.TranscribedText);
    }
}
