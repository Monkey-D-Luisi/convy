using Convy.Application.Features.Households.Commands;
using FluentValidation.TestHelper;

namespace Convy.Application.Tests.Features.Households;

public class CreateHouseholdCommandValidatorTests
{
    private readonly CreateHouseholdCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidData_PassesValidation()
    {
        var command = new CreateHouseholdCommand("My Household");
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyName_FailsValidation()
    {
        var command = new CreateHouseholdCommand("");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WithLongName_FailsValidation()
    {
        var command = new CreateHouseholdCommand(new string('a', 101));
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }
}
