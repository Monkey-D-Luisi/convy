using Convy.Domain.Entities;
using FluentAssertions;

namespace Convy.Domain.Tests.Entities;

public class NotificationPreferencesTests
{
    [Fact]
    public void CreateDefault_WithValidUserId_UsesHighSignalDefaults()
    {
        var userId = Guid.NewGuid();

        var preferences = NotificationPreferences.CreateDefault(userId);

        preferences.UserId.Should().Be(userId);
        preferences.ItemsAdded.Should().BeTrue();
        preferences.TasksAdded.Should().BeTrue();
        preferences.ItemsCompleted.Should().BeFalse();
        preferences.TasksCompleted.Should().BeFalse();
        preferences.ItemTaskChanges.Should().BeFalse();
        preferences.ListChanges.Should().BeTrue();
        preferences.MemberChanges.Should().BeTrue();
    }

    [Fact]
    public void Update_WithNewValues_ReplacesAllCategories()
    {
        var preferences = NotificationPreferences.CreateDefault(Guid.NewGuid());

        preferences.Update(
            itemsAdded: false,
            tasksAdded: false,
            itemsCompleted: true,
            tasksCompleted: true,
            itemTaskChanges: true,
            listChanges: false,
            memberChanges: false);

        preferences.ItemsAdded.Should().BeFalse();
        preferences.TasksAdded.Should().BeFalse();
        preferences.ItemsCompleted.Should().BeTrue();
        preferences.TasksCompleted.Should().BeTrue();
        preferences.ItemTaskChanges.Should().BeTrue();
        preferences.ListChanges.Should().BeFalse();
        preferences.MemberChanges.Should().BeFalse();
    }

    [Fact]
    public void CreateDefault_WithEmptyUserId_ThrowsArgumentException()
    {
        var act = () => NotificationPreferences.CreateDefault(Guid.Empty);

        act.Should().Throw<ArgumentException>();
    }
}
