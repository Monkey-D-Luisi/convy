using Convy.Application.Features.Items.Commands;
using FluentValidation.TestHelper;

namespace Convy.Application.Tests.Features.Items;

public class CreateItemCommandValidatorTests
{
    private readonly CreateItemCommandValidator _validator = new();

    [Fact]
    public void Validate_WithAllValidFields_PassesValidation()
    {
        var command = new CreateItemCommand(Guid.NewGuid(), "Milk", 2, "liters", "Whole milk", null, null);
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithOnlyRequiredFields_PassesValidation()
    {
        var command = new CreateItemCommand(Guid.NewGuid(), "Milk", null, null, null, null, null);
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyListId_FailsValidation()
    {
        var command = new CreateItemCommand(Guid.Empty, "Milk", null, null, null, null, null);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ListId);
    }

    [Fact]
    public void Validate_WithEmptyTitle_FailsValidation()
    {
        var command = new CreateItemCommand(Guid.NewGuid(), "", null, null, null, null, null);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Validate_WithLongTitle_FailsValidation()
    {
        var command = new CreateItemCommand(Guid.NewGuid(), new string('a', 201), null, null, null, null, null);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Validate_WithZeroQuantity_FailsValidation()
    {
        var command = new CreateItemCommand(Guid.NewGuid(), "Milk", 0, null, null, null, null);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Quantity);
    }

    [Fact]
    public void Validate_WithNegativeQuantity_FailsValidation()
    {
        var command = new CreateItemCommand(Guid.NewGuid(), "Milk", -1, null, null, null, null);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Quantity);
    }

    [Fact]
    public void Validate_WithNullQuantity_PassesValidation()
    {
        var command = new CreateItemCommand(Guid.NewGuid(), "Milk", null, null, null, null, null);
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Quantity);
    }

    [Fact]
    public void Validate_WithLongUnit_FailsValidation()
    {
        var command = new CreateItemCommand(Guid.NewGuid(), "Milk", 1, new string('a', 51), null, null, null);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Unit);
    }

    [Fact]
    public void Validate_WithLongNote_FailsValidation()
    {
        var command = new CreateItemCommand(Guid.NewGuid(), "Milk", 1, null, new string('a', 501), null, null);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Note);
    }
}
