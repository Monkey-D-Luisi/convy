using Convy.Application.Features.Invites.Commands;
using FluentValidation.TestHelper;

namespace Convy.Application.Tests.Features.Invites;

public class JoinHouseholdCommandValidatorTests
{
    private readonly JoinHouseholdCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidData_PassesValidation()
    {
        var command = new JoinHouseholdCommand("ABC123");
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyInviteCode_FailsValidation()
    {
        var command = new JoinHouseholdCommand("");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.InviteCode);
    }

    [Fact]
    public void Validate_WithLongInviteCode_FailsValidation()
    {
        var command = new JoinHouseholdCommand(new string('a', 21));
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.InviteCode);
    }
}
