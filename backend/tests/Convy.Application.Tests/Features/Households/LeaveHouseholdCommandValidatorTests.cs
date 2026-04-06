using Convy.Application.Features.Households.Commands;
using FluentValidation.TestHelper;

namespace Convy.Application.Tests.Features.Households;

public class LeaveHouseholdCommandValidatorTests
{
    private readonly LeaveHouseholdCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidData_PassesValidation()
    {
        var command = new LeaveHouseholdCommand(Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyHouseholdId_FailsValidation()
    {
        var command = new LeaveHouseholdCommand(Guid.Empty);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.HouseholdId);
    }
}
