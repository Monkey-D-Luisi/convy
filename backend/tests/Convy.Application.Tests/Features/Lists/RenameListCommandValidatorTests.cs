using Convy.Application.Features.Lists.Commands;
using FluentValidation.TestHelper;

namespace Convy.Application.Tests.Features.Lists;

public class RenameListCommandValidatorTests
{
    private readonly RenameListCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidData_PassesValidation()
    {
        var command = new RenameListCommand(Guid.NewGuid(), "New Name");
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyListId_FailsValidation()
    {
        var command = new RenameListCommand(Guid.Empty, "New Name");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ListId);
    }

    [Fact]
    public void Validate_WithEmptyNewName_FailsValidation()
    {
        var command = new RenameListCommand(Guid.NewGuid(), "");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.NewName);
    }

    [Fact]
    public void Validate_WithLongNewName_FailsValidation()
    {
        var command = new RenameListCommand(Guid.NewGuid(), new string('a', 101));
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.NewName);
    }
}
