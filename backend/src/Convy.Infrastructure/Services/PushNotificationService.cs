using Convy.Application.Common.Interfaces;
using Convy.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Convy.Infrastructure.Services;

public class PushNotificationService : IPushNotificationService
{
    private readonly IDeviceTokenRepository _deviceTokenRepository;
    private readonly IFirebaseMessagingClient _messagingClient;
    private readonly ILogger<PushNotificationService> _logger;

    public PushNotificationService(
        IDeviceTokenRepository deviceTokenRepository,
        IFirebaseMessagingClient messagingClient,
        ILogger<PushNotificationService> logger)
    {
        _deviceTokenRepository = deviceTokenRepository;
        _messagingClient = messagingClient;
        _logger = logger;
    }

    public async Task SendToUsersAsync(IEnumerable<Guid> userIds, string title, string body, Dictionary<string, string>? data = null, CancellationToken cancellationToken = default)
    {
        var tokens = await _deviceTokenRepository.GetByUserIdsAsync(userIds, cancellationToken);
        if (tokens.Count == 0) return;

        try
        {
            var response = await _messagingClient.SendMulticastAsync(
                tokens.Select(t => t.Token).ToList(),
                title,
                body,
                data,
                cancellationToken);

            if (response.Failures.Count == 0) return;

            var tokenByValue = tokens.ToDictionary(t => t.Token);
            foreach (var group in response.Failures.GroupBy(f => f.ErrorCode))
            {
                _logger.LogWarning(
                    "FCM failed {FailureCount}/{Total} messages with {ErrorCode}",
                    group.Count(),
                    tokens.Count,
                    group.Key);
            }

            var removedAny = false;
            foreach (var failure in response.Failures.Where(IsInvalidTokenFailure))
            {
                if (!tokenByValue.TryGetValue(failure.Token, out var token)) continue;

                _deviceTokenRepository.Remove(token);
                removedAny = true;
            }

            if (removedAny)
            {
                await _deviceTokenRepository.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send FCM notifications");
        }
    }

    private static bool IsInvalidTokenFailure(FirebaseSendFailure failure) =>
        failure.ErrorCode is "Unregistered" or "SenderIdMismatch";
}
