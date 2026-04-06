using Convy.Application.Common.Interfaces;
using Convy.Domain.Repositories;
using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Logging;

namespace Convy.Infrastructure.Services;

public class PushNotificationService : IPushNotificationService
{
    private readonly IDeviceTokenRepository _deviceTokenRepository;
    private readonly ILogger<PushNotificationService> _logger;

    public PushNotificationService(IDeviceTokenRepository deviceTokenRepository, ILogger<PushNotificationService> logger)
    {
        _deviceTokenRepository = deviceTokenRepository;
        _logger = logger;
    }

    public async Task SendToUsersAsync(IEnumerable<Guid> userIds, string title, string body, Dictionary<string, string>? data = null, CancellationToken cancellationToken = default)
    {
        var tokens = await _deviceTokenRepository.GetByUserIdsAsync(userIds, cancellationToken);
        if (tokens.Count == 0) return;

        var message = new MulticastMessage
        {
            Tokens = tokens.Select(t => t.Token).ToList(),
            Notification = new Notification
            {
                Title = title,
                Body = body,
            },
            Data = data,
        };

        try
        {
            var response = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(message, cancellationToken);
            if (response.FailureCount > 0)
            {
                _logger.LogWarning("FCM: {FailureCount}/{Total} messages failed", response.FailureCount, tokens.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send FCM notifications");
        }
    }
}
