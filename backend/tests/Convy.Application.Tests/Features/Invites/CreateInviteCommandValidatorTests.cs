using Convy.Application.Features.Invites.Commands;
using FluentValidation.TestHelper;

namespace Convy.Application.Tests.Features.Invites;

public class CreateInviteCommandValidatorTests
{
    private readonly CreateInviteCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidData_PassesValidation()
    {
        var command = new CreateInviteCommand(Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyHouseholdId_FailsValidation()
    {
        var command = new CreateInviteCommand(Guid.Empty);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.HouseholdId);
    }
}
