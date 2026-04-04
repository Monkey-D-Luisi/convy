using Convy.Domain.Entities;
using FluentAssertions;

namespace Convy.Domain.Tests.Entities;

public class UserTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesUser()
    {
        // Arrange & Act
        var user = new User("firebase123", "John Doe", "john@example.com");

        // Assert
        user.FirebaseUid.Should().Be("firebase123");
        user.DisplayName.Should().Be("John Doe");
        user.Email.Should().Be("john@example.com");
        user.Id.Should().NotBeEmpty();
        user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidFirebaseUid_ThrowsArgumentException(string? uid)
    {
        var act = () => new User(uid!, "Name", "email@test.com");
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Constructor_WithInvalidDisplayName_ThrowsArgumentException(string? name)
    {
        var act = () => new User("uid", name!, "email@test.com");
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Constructor_WithInvalidEmail_ThrowsArgumentException(string? email)
    {
        var act = () => new User("uid", "Name", email!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateProfile_WithValidName_UpdatesDisplayName()
    {
        // Arrange
        var user = new User("uid", "Old Name", "test@test.com");

        // Act
        user.UpdateProfile("New Name");

        // Assert
        user.DisplayName.Should().Be("New Name");
    }

    [Fact]
    public void UpdateProfile_WithEmptyName_ThrowsArgumentException()
    {
        var user = new User("uid", "Name", "test@test.com");
        var act = () => user.UpdateProfile("");
        act.Should().Throw<ArgumentException>();
    }
}
