using Convy.Application.Features.Lists.Commands;
using FluentValidation.TestHelper;

namespace Convy.Application.Tests.Features.Lists;

public class ArchiveListCommandValidatorTests
{
    private readonly ArchiveListCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidData_PassesValidation()
    {
        var command = new ArchiveListCommand(Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyListId_FailsValidation()
    {
        var command = new ArchiveListCommand(Guid.Empty);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ListId);
    }
}
