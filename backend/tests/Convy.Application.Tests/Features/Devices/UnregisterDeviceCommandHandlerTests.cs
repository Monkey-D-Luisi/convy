using Convy.Application.Features.Devices.Commands;
using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using FluentAssertions;
using NSubstitute;

namespace Convy.Application.Tests.Features.Devices;

public class UnregisterDeviceCommandHandlerTests
{
    private readonly IDeviceTokenRepository _deviceTokenRepository = Substitute.For<IDeviceTokenRepository>();
    private readonly UnregisterDeviceCommandHandler _handler;

    public UnregisterDeviceCommandHandlerTests()
    {
        _handler = new UnregisterDeviceCommandHandler(_deviceTokenRepository);
    }

    [Fact]
    public async Task Handle_WhenTokenExists_RemovesToken()
    {
        // Arrange
        var existing = new DeviceToken(Guid.NewGuid(), "fcm-token-123", "android");
        _deviceTokenRepository.GetByTokenAsync("fcm-token-123", Arg.Any<CancellationToken>())
            .Returns(existing);

        var command = new UnregisterDeviceCommand("fcm-token-123");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _deviceTokenRepository.Received(1).Remove(existing);
        await _deviceTokenRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTokenNotFound_ReturnsSuccess()
    {
        // Arrange
        _deviceTokenRepository.GetByTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((DeviceToken?)null);

        var command = new UnregisterDeviceCommand("nonexistent-token");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _deviceTokenRepository.DidNotReceive().Remove(Arg.Any<DeviceToken>());
    }
}
