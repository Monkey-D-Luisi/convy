using Convy.Application.Features.Items.Commands;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace Convy.Application.Tests.Features.Items;

public class BatchCreateItemsCommandValidatorTests
{
    private readonly BatchCreateItemsCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidItems_PassesValidation()
    {
        var command = new BatchCreateItemsCommand(Guid.NewGuid(), new List<BatchItemDto>
        {
            new("Milk", 2, "liters", null),
            new("Bread", null, null, "Whole wheat")
        });
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyListId_FailsValidation()
    {
        var command = new BatchCreateItemsCommand(Guid.Empty, new List<BatchItemDto>
        {
            new("Milk", null, null, null)
        });
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ListId);
    }

    [Fact]
    public void Validate_WithEmptyItems_FailsValidation()
    {
        var command = new BatchCreateItemsCommand(Guid.NewGuid(), new List<BatchItemDto>());
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Items);
    }

    [Fact]
    public void Validate_WithTooManyItems_FailsValidation()
    {
        var items = Enumerable.Range(0, 21).Select(i => new BatchItemDto($"Item {i}", null, null, null)).ToList();
        var command = new BatchCreateItemsCommand(Guid.NewGuid(), items);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Items);
    }

    [Fact]
    public void Validate_With20Items_PassesValidation()
    {
        var items = Enumerable.Range(0, 20).Select(i => new BatchItemDto($"Item {i}", null, null, null)).ToList();
        var command = new BatchCreateItemsCommand(Guid.NewGuid(), items);
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyItemTitle_FailsValidation()
    {
        var command = new BatchCreateItemsCommand(Guid.NewGuid(), new List<BatchItemDto>
        {
            new("", null, null, null)
        });
        var result = _validator.TestValidate(command);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithLongItemTitle_FailsValidation()
    {
        var command = new BatchCreateItemsCommand(Guid.NewGuid(), new List<BatchItemDto>
        {
            new(new string('a', 201), null, null, null)
        });
        var result = _validator.TestValidate(command);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithZeroQuantity_FailsValidation()
    {
        var command = new BatchCreateItemsCommand(Guid.NewGuid(), new List<BatchItemDto>
        {
            new("Milk", 0, null, null)
        });
        var result = _validator.TestValidate(command);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithNegativeQuantity_FailsValidation()
    {
        var command = new BatchCreateItemsCommand(Guid.NewGuid(), new List<BatchItemDto>
        {
            new("Milk", -1, null, null)
        });
        var result = _validator.TestValidate(command);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithLongUnit_FailsValidation()
    {
        var command = new BatchCreateItemsCommand(Guid.NewGuid(), new List<BatchItemDto>
        {
            new("Milk", 1, new string('a', 51), null)
        });
        var result = _validator.TestValidate(command);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithLongNote_FailsValidation()
    {
        var command = new BatchCreateItemsCommand(Guid.NewGuid(), new List<BatchItemDto>
        {
            new("Milk", null, null, new string('a', 501))
        });
        var result = _validator.TestValidate(command);
        result.IsValid.Should().BeFalse();
    }
}
