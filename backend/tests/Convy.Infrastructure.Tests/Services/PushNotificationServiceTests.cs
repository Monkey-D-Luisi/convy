using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using Convy.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Convy.Infrastructure.Tests.Services;

public class PushNotificationServiceTests
{
    private readonly IDeviceTokenRepository _deviceTokenRepository = Substitute.For<IDeviceTokenRepository>();
    private readonly IFirebaseMessagingClient _messagingClient = Substitute.For<IFirebaseMessagingClient>();
    private readonly ILogger<PushNotificationService> _logger = Substitute.For<ILogger<PushNotificationService>>();
    private readonly PushNotificationService _service;

    public PushNotificationServiceTests()
    {
        _service = new PushNotificationService(_deviceTokenRepository, _messagingClient, _logger);
    }

    [Fact]
    public async Task SendToUsersAsync_WhenFcmReportsUnregistered_RemovesInvalidToken()
    {
        var userId = Guid.NewGuid();
        var token = new DeviceToken(userId, "stale-token", "Android");
        _deviceTokenRepository.GetByUserIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([token]);
        _messagingClient.SendMulticastAsync(
                Arg.Any<IReadOnlyList<string>>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<IReadOnlyDictionary<string, string>?>(),
                Arg.Any<CancellationToken>())
            .Returns(new FirebaseMulticastSendResult(
                SuccessCount: 0,
                Failures: [new FirebaseSendFailure("stale-token", "Unregistered", "Token is not registered")]));

        await _service.SendToUsersAsync([userId], "Item completed", "Milk was marked as done");

        _deviceTokenRepository.Received(1).Remove(token);
        await _deviceTokenRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendToUsersAsync_WhenFcmReportsPermissionDenied_DoesNotRemoveToken()
    {
        var userId = Guid.NewGuid();
        var token = new DeviceToken(userId, "valid-token", "Android");
        _deviceTokenRepository.GetByUserIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([token]);
        _messagingClient.SendMulticastAsync(
                Arg.Any<IReadOnlyList<string>>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<IReadOnlyDictionary<string, string>?>(),
                Arg.Any<CancellationToken>())
            .Returns(new FirebaseMulticastSendResult(
                SuccessCount: 0,
                Failures: [new FirebaseSendFailure("valid-token", "PermissionDenied", "Missing IAM permission")]));

        await _service.SendToUsersAsync([userId], "Item completed", "Milk was marked as done");

        _deviceTokenRepository.DidNotReceive().Remove(Arg.Any<DeviceToken>());
        await _deviceTokenRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendToUsersAsync_PassesNotificationPayloadToFcmClient()
    {
        var userId = Guid.NewGuid();
        var token = new DeviceToken(userId, "valid-token", "Android");
        var data = new Dictionary<string, string> { ["listId"] = "list-1" };
        _deviceTokenRepository.GetByUserIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([token]);
        _messagingClient.SendMulticastAsync(
                Arg.Any<IReadOnlyList<string>>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<IReadOnlyDictionary<string, string>?>(),
                Arg.Any<CancellationToken>())
            .Returns(new FirebaseMulticastSendResult(1, []));

        await _service.SendToUsersAsync([userId], "Item completed", "Milk was marked as done", data);

        await _messagingClient.Received(1).SendMulticastAsync(
            Arg.Is<IReadOnlyList<string>>(tokens => tokens.SequenceEqual(new[] { "valid-token" })),
            "Item completed",
            "Milk was marked as done",
            Arg.Is<IReadOnlyDictionary<string, string>?>(payload => payload != null && payload["listId"] == "list-1"),
            Arg.Any<CancellationToken>());
    }
}
