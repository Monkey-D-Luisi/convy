using Convy.Application.Features.Households.Commands;
using FluentValidation.TestHelper;

namespace Convy.Application.Tests.Features.Households;

public class RenameHouseholdCommandValidatorTests
{
    private readonly RenameHouseholdCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidData_PassesValidation()
    {
        var command = new RenameHouseholdCommand(Guid.NewGuid(), "New Name");
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyHouseholdId_FailsValidation()
    {
        var command = new RenameHouseholdCommand(Guid.Empty, "New Name");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.HouseholdId);
    }

    [Fact]
    public void Validate_WithEmptyNewName_FailsValidation()
    {
        var command = new RenameHouseholdCommand(Guid.NewGuid(), "");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.NewName);
    }

    [Fact]
    public void Validate_WithLongName_FailsValidation()
    {
        var command = new RenameHouseholdCommand(Guid.NewGuid(), new string('a', 101));
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.NewName);
    }
}
