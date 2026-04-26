using Convy.Application.Features.Users.Commands;
using FluentAssertions;

namespace Convy.Application.Tests.Features.Users;

public class UpdateNotificationPreferencesCommandValidatorTests
{
    private readonly UpdateNotificationPreferencesCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_IsValid()
    {
        var command = new UpdateNotificationPreferencesCommand(
            ItemsAdded: true,
            TasksAdded: true,
            ItemsCompleted: false,
            TasksCompleted: false,
            ItemTaskChanges: false,
            ListChanges: true,
            MemberChanges: true);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
