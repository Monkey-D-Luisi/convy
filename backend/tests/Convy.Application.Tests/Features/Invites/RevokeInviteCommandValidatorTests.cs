using Convy.Application.Features.Invites.Commands;
using FluentValidation.TestHelper;

namespace Convy.Application.Tests.Features.Invites;

public class RevokeInviteCommandValidatorTests
{
    private readonly RevokeInviteCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidData_PassesValidation()
    {
        var command = new RevokeInviteCommand(Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyInviteId_FailsValidation()
    {
        var command = new RevokeInviteCommand(Guid.Empty);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.InviteId);
    }
}
