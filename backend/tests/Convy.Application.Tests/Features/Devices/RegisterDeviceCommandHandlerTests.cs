using Convy.Application.Common.Interfaces;
using Convy.Application.Features.Devices.Commands;
using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using FluentAssertions;
using NSubstitute;

namespace Convy.Application.Tests.Features.Devices;

public class RegisterDeviceCommandHandlerTests
{
    private readonly IDeviceTokenRepository _deviceTokenRepository = Substitute.For<IDeviceTokenRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly RegisterDeviceCommandHandler _handler;
    private readonly Guid _userId = Guid.NewGuid();

    public RegisterDeviceCommandHandlerTests()
    {
        _currentUser.UserId.Returns(_userId);
        _handler = new RegisterDeviceCommandHandler(_deviceTokenRepository, _currentUser);
    }

    [Fact]
    public async Task Handle_WithNewToken_RegistersSuccessfully()
    {
        // Arrange
        _deviceTokenRepository.GetByTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((DeviceToken?)null);

        var command = new RegisterDeviceCommand("fcm-token-123", "android", "es-ES");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _deviceTokenRepository.Received(1).AddAsync(
            Arg.Is<DeviceToken>(token => token.Locale == "es"),
            Arg.Any<CancellationToken>());
        await _deviceTokenRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithExistingToken_ReturnsSuccess()
    {
        // Arrange
        var existing = new DeviceToken(_userId, "fcm-token-123", "android");
        _deviceTokenRepository.GetByTokenAsync("fcm-token-123", Arg.Any<CancellationToken>())
            .Returns(existing);

        var command = new RegisterDeviceCommand("fcm-token-123", "android", "es-ES");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _deviceTokenRepository.DidNotReceive().AddAsync(Arg.Any<DeviceToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithExistingTokenForDifferentUser_ReassignsExistingToken()
    {
        // Arrange
        var existing = new DeviceToken(Guid.NewGuid(), "fcm-token-123", "android");
        _deviceTokenRepository.GetByTokenAsync("fcm-token-123", Arg.Any<CancellationToken>())
            .Returns(existing);

        var command = new RegisterDeviceCommand("fcm-token-123", "android", "es-ES");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        existing.UserId.Should().Be(_userId);
        existing.Token.Should().Be("fcm-token-123");
        existing.Locale.Should().Be("es");
        _deviceTokenRepository.DidNotReceive().Remove(Arg.Any<DeviceToken>());
        await _deviceTokenRepository.DidNotReceive().AddAsync(Arg.Any<DeviceToken>(), Arg.Any<CancellationToken>());
        await _deviceTokenRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
