using Convy.Application.Features.Lists.Commands;
using Convy.Domain.ValueObjects;
using FluentValidation.TestHelper;

namespace Convy.Application.Tests.Features.Lists;

public class CreateListCommandValidatorTests
{
    private readonly CreateListCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidData_PassesValidation()
    {
        var command = new CreateListCommand(Guid.NewGuid(), "Groceries", ListType.Shopping);
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyHouseholdId_FailsValidation()
    {
        var command = new CreateListCommand(Guid.Empty, "Groceries", ListType.Shopping);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.HouseholdId);
    }

    [Fact]
    public void Validate_WithEmptyName_FailsValidation()
    {
        var command = new CreateListCommand(Guid.NewGuid(), "", ListType.Shopping);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WithLongName_FailsValidation()
    {
        var command = new CreateListCommand(Guid.NewGuid(), new string('a', 101), ListType.Shopping);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WithInvalidEnum_FailsValidation()
    {
        var command = new CreateListCommand(Guid.NewGuid(), "Groceries", (ListType)999);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Type);
    }
}
