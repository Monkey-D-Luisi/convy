using Convy.Application.Common.Models;
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
    private readonly INotificationPreferencesRepository _preferencesRepository = Substitute.For<INotificationPreferencesRepository>();
    private readonly IFirebaseMessagingClient _messagingClient = Substitute.For<IFirebaseMessagingClient>();
    private readonly ILogger<PushNotificationService> _logger = Substitute.For<ILogger<PushNotificationService>>();
    private readonly PushNotificationService _service;

    public PushNotificationServiceTests()
    {
        _service = new PushNotificationService(
            _deviceTokenRepository,
            _preferencesRepository,
            _messagingClient,
            new PushNotificationTextProvider(),
            _logger);
    }

    [Fact]
    public async Task SendLocalizedAsync_WhenFcmReportsUnregistered_RemovesInvalidToken()
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

        await _service.SendLocalizedAsync(
            [userId],
            NotificationCategory.ItemsAdded,
            CreateItemsAddedTemplate(),
            data: null,
            CancellationToken.None);

        _deviceTokenRepository.Received(1).Remove(token);
        await _deviceTokenRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendLocalizedAsync_WhenFcmReportsPermissionDenied_DoesNotRemoveToken()
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

        await _service.SendLocalizedAsync(
            [userId],
            NotificationCategory.ItemsAdded,
            CreateItemsAddedTemplate(),
            data: null,
            CancellationToken.None);

        _deviceTokenRepository.DidNotReceive().Remove(Arg.Any<DeviceToken>());
        await _deviceTokenRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendLocalizedAsync_PassesRenderedPayloadToFcmClient()
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

        await _service.SendLocalizedAsync(
            [userId],
            NotificationCategory.ItemsAdded,
            CreateItemsAddedTemplate(),
            data,
            CancellationToken.None);

        await _messagingClient.Received(1).SendMulticastAsync(
            Arg.Is<IReadOnlyList<string>>(tokens => tokens.SequenceEqual(new[] { "valid-token" })),
            "Items added",
            "Luis added 1 item to Groceries",
            Arg.Is<IReadOnlyDictionary<string, string>?>(payload => payload != null && payload["listId"] == "list-1"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendLocalizedAsync_WhenCompletionCategoryUsesDefaultPreferences_DoesNotSend()
    {
        var userId = Guid.NewGuid();
        var token = new DeviceToken(userId, "es-token", "Android", "es-ES");
        _deviceTokenRepository.GetByUserIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([token]);
        _preferencesRepository.GetByUserIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([]);

        await _service.SendLocalizedAsync(
            [userId],
            NotificationCategory.ItemsCompleted,
            new PushNotificationTemplate(
                NotificationTemplateKey.ItemsCompleted,
                new Dictionary<string, string>
                {
                    ["actorName"] = "Luis",
                    ["listName"] = "Groceries",
                    ["count"] = "2",
                }),
            data: null,
            CancellationToken.None);

        await _messagingClient.DidNotReceive().SendMulticastAsync(
            Arg.Any<IReadOnlyList<string>>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<IReadOnlyDictionary<string, string>?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendLocalizedAsync_GroupsEnabledRecipientsByLocale()
    {
        var englishUserId = Guid.NewGuid();
        var spanishUserId = Guid.NewGuid();
        _deviceTokenRepository.GetByUserIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([
                new DeviceToken(englishUserId, "en-token", "Android", "en-US"),
                new DeviceToken(spanishUserId, "es-token", "Android", "es-ES"),
            ]);
        _preferencesRepository.GetByUserIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _messagingClient.SendMulticastAsync(
                Arg.Any<IReadOnlyList<string>>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<IReadOnlyDictionary<string, string>?>(),
                Arg.Any<CancellationToken>())
            .Returns(new FirebaseMulticastSendResult(1, []));

        await _service.SendLocalizedAsync(
            [englishUserId, spanishUserId],
            NotificationCategory.ItemsAdded,
            new PushNotificationTemplate(
                NotificationTemplateKey.ItemsAdded,
                new Dictionary<string, string>
                {
                    ["actorName"] = "Luis",
                    ["listName"] = "Groceries",
                    ["count"] = "4",
                }),
            new Dictionary<string, string> { ["listId"] = "list-1" },
            CancellationToken.None);

        await _messagingClient.Received(1).SendMulticastAsync(
            Arg.Is<IReadOnlyList<string>>(tokens => tokens.SequenceEqual(new[] { "en-token" })),
            "Items added",
            "Luis added 4 items to Groceries",
            Arg.Is<IReadOnlyDictionary<string, string>?>(payload => payload != null && payload["listId"] == "list-1"),
            Arg.Any<CancellationToken>());
        await _messagingClient.Received(1).SendMulticastAsync(
            Arg.Is<IReadOnlyList<string>>(tokens => tokens.SequenceEqual(new[] { "es-token" })),
            "Artículos añadidos",
            "Luis añadió 4 artículos a Groceries",
            Arg.Is<IReadOnlyDictionary<string, string>?>(payload => payload != null && payload["listId"] == "list-1"),
            Arg.Any<CancellationToken>());
    }

    private static PushNotificationTemplate CreateItemsAddedTemplate() =>
        new(
            NotificationTemplateKey.ItemsAdded,
            new Dictionary<string, string>
            {
                ["actorName"] = "Luis",
                ["listName"] = "Groceries",
                ["count"] = "1",
            });
}
