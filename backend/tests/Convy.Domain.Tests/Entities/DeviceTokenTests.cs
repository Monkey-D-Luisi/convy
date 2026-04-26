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
        token.Locale.Should().Be("en");
        token.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        token.Id.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("es-ES", "es")]
    [InlineData("es", "es")]
    [InlineData("en-US", "en")]
    [InlineData("pl-PL", "en")]
    [InlineData(null, "en")]
    public void Constructor_WithLocale_NormalizesSupportedLanguageOrFallsBackToEnglish(string? locale, string expected)
    {
        var token = new DeviceToken(Guid.NewGuid(), "fcm-token-123", "android", locale);

        token.Locale.Should().Be(expected);
    }

    [Fact]
    public void Constructor_WithEmptyUserId_ThrowsArgumentException()
    {
        // Act
        var act = () => new DeviceToken(Guid.Empty, "fcm-token-123", "android");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ReassignTo_WithValidData_UpdatesOwnerAndPlatform()
    {
        // Arrange
        var token = new DeviceToken(Guid.NewGuid(), "fcm-token-123", "android");
        var newUserId = Guid.NewGuid();

        // Act
        token.ReassignTo(newUserId, "ios");

        // Assert
        token.UserId.Should().Be(newUserId);
        token.Token.Should().Be("fcm-token-123");
        token.Platform.Should().Be("ios");
        token.Locale.Should().Be("en");
    }

    [Fact]
    public void ReassignTo_WithLocale_UpdatesOwnerPlatformAndLocale()
    {
        var token = new DeviceToken(Guid.NewGuid(), "fcm-token-123", "android", "en");
        var newUserId = Guid.NewGuid();

        token.ReassignTo(newUserId, "ios", "es-ES");

        token.UserId.Should().Be(newUserId);
        token.Platform.Should().Be("ios");
        token.Locale.Should().Be("es");
    }

    [Fact]
    public void ReassignTo_WithEmptyUserId_ThrowsArgumentException()
    {
        // Arrange
        var token = new DeviceToken(Guid.NewGuid(), "fcm-token-123", "android");

        // Act
        var act = () => token.ReassignTo(Guid.Empty, "android");

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

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ReassignTo_WithEmptyPlatform_ThrowsArgumentException(string? platform)
    {
        // Arrange
        var token = new DeviceToken(Guid.NewGuid(), "fcm-token-123", "android");

        // Act
        var act = () => token.ReassignTo(Guid.NewGuid(), platform!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
