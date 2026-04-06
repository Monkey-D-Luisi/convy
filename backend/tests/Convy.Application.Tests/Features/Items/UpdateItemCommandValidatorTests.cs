using Convy.Application.Features.Items.Commands;
using FluentValidation.TestHelper;

namespace Convy.Application.Tests.Features.Items;

public class UpdateItemCommandValidatorTests
{
    private readonly UpdateItemCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidData_PassesValidation()
    {
        var command = new UpdateItemCommand(Guid.NewGuid(), "Milk", 2, "liters", "Whole milk", null, null);
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyItemId_FailsValidation()
    {
        var command = new UpdateItemCommand(Guid.Empty, "Milk", null, null, null, null, null);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ItemId);
    }

    [Fact]
    public void Validate_WithEmptyTitle_FailsValidation()
    {
        var command = new UpdateItemCommand(Guid.NewGuid(), "", null, null, null, null, null);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Validate_WithLongTitle_FailsValidation()
    {
        var command = new UpdateItemCommand(Guid.NewGuid(), new string('a', 201), null, null, null, null, null);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Validate_WithZeroQuantity_FailsValidation()
    {
        var command = new UpdateItemCommand(Guid.NewGuid(), "Milk", 0, null, null, null, null);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Quantity);
    }

    [Fact]
    public void Validate_WithNullQuantity_PassesValidation()
    {
        var command = new UpdateItemCommand(Guid.NewGuid(), "Milk", null, null, null, null, null);
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Quantity);
    }

    [Fact]
    public void Validate_WithLongUnit_FailsValidation()
    {
        var command = new UpdateItemCommand(Guid.NewGuid(), "Milk", 1, new string('a', 51), null, null, null);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Unit);
    }

    [Fact]
    public void Validate_WithLongNote_FailsValidation()
    {
        var command = new UpdateItemCommand(Guid.NewGuid(), "Milk", 1, null, new string('a', 501), null, null);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Note);
    }
}
