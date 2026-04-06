using Convy.Domain.Entities;
using FluentAssertions;

namespace Convy.Domain.Tests.Entities;

public class DeviceTokenTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesToken()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var token = new DeviceToken(userId, "fcm-token-123", "android");

        // Assert
        token.UserId.Should().Be(userId);
        token.Token.Should().Be("fcm-token-123");
        token.Platform.Should().Be("android");
        token.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        token.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Constructor_WithEmptyUserId_ThrowsArgumentException()
    {
        // Act
        var act = () => new DeviceToken(Guid.Empty, "fcm-token-123", "android");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyToken_ThrowsArgumentException(string? token)
    {
        // Act
        var act = () => new DeviceToken(Guid.NewGuid(), token!, "android");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyPlatform_ThrowsArgumentException(string? platform)
    {
        // Act
        var act = () => new DeviceToken(Guid.NewGuid(), "fcm-token-123", platform!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
