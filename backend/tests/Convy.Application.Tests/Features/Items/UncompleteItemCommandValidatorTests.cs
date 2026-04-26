using Convy.Application.Features.Items.Commands;
using FluentValidation.TestHelper;

namespace Convy.Application.Tests.Features.Items;

public class UncompleteItemCommandValidatorTests
{
    private readonly UncompleteItemCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidData_PassesValidation()
    {
        var command = new UncompleteItemCommand(Guid.NewGuid(), Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyListId_FailsValidation()
    {
        var command = new UncompleteItemCommand(Guid.Empty, Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ListId);
    }

    [Fact]
    public void Validate_WithEmptyItemId_FailsValidation()
    {
        var command = new UncompleteItemCommand(Guid.NewGuid(), Guid.Empty);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ItemId);
    }
}
