using Convy.Application.Common.Interfaces;
using Convy.Application.Common.Models;
using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Convy.Infrastructure.Services;

public class PushNotificationService : IPushNotificationService
{
    private readonly IDeviceTokenRepository _deviceTokenRepository;
    private readonly INotificationPreferencesRepository _preferencesRepository;
    private readonly IFirebaseMessagingClient _messagingClient;
    private readonly IPushNotificationTextProvider _textProvider;
    private readonly ILogger<PushNotificationService> _logger;

    public PushNotificationService(
        IDeviceTokenRepository deviceTokenRepository,
        INotificationPreferencesRepository preferencesRepository,
        IFirebaseMessagingClient messagingClient,
        IPushNotificationTextProvider textProvider,
        ILogger<PushNotificationService> logger)
    {
        _deviceTokenRepository = deviceTokenRepository;
        _preferencesRepository = preferencesRepository;
        _messagingClient = messagingClient;
        _textProvider = textProvider;
        _logger = logger;
    }

    public async Task SendLocalizedAsync(
        IEnumerable<Guid> userIds,
        NotificationCategory category,
        PushNotificationTemplate template,
        Dictionary<string, string>? data = null,
        CancellationToken cancellationToken = default)
    {
        var requestedUserIds = userIds.Where(id => id != Guid.Empty).Distinct().ToList();
        if (requestedUserIds.Count == 0) return;

        var tokens = await _deviceTokenRepository.GetByUserIdsAsync(requestedUserIds, cancellationToken);
        if (tokens.Count == 0) return;

        var preferences = await _preferencesRepository.GetByUserIdsAsync(
            tokens.Select(t => t.UserId).Distinct(),
            cancellationToken);
        var preferencesByUserId = preferences.ToDictionary(p => p.UserId);
        var preferenceCategory = category.ToPreferenceCategory();

        var enabledTokens = tokens
            .Where(token => IsEnabled(token.UserId, preferenceCategory, preferencesByUserId))
            .ToList();

        _logger.LogInformation(
            "Preparing localized push notification {Category}: requestedUsers={RequestedUserCount}, tokens={TokenCount}, enabledTokens={EnabledTokenCount}, localeGroups={LocaleGroupCount}",
            category,
            requestedUserIds.Count,
            tokens.Count,
            enabledTokens.Count,
            enabledTokens.Select(t => t.Locale).Distinct().Count());

        if (enabledTokens.Count == 0) return;

        var removedAny = false;
        foreach (var localeGroup in enabledTokens.GroupBy(t => DeviceToken.NormalizeLocale(t.Locale)))
        {
            var rendered = _textProvider.Render(template, localeGroup.Key);
            removedAny |= await SendToTokenGroupAsync(
                localeGroup.ToList(),
                category,
                localeGroup.Key,
                rendered.Title,
                rendered.Body,
                data,
                cancellationToken);
        }

        if (removedAny)
        {
            await _deviceTokenRepository.SaveChangesAsync(cancellationToken);
        }
    }

    private static bool IsInvalidTokenFailure(FirebaseSendFailure failure) =>
        failure.ErrorCode is "Unregistered" or "SenderIdMismatch";

    private static bool IsEnabled(
        Guid userId,
        NotificationPreferenceCategory category,
        IReadOnlyDictionary<Guid, NotificationPreferences> preferencesByUserId)
    {
        var preferences = preferencesByUserId.TryGetValue(userId, out var existing)
            ? existing
            : NotificationPreferences.CreateDefault(userId);

        return preferences.IsEnabled(category);
    }

    private async Task<bool> SendToTokenGroupAsync(
        IReadOnlyList<DeviceToken> tokens,
        NotificationCategory category,
        string locale,
        string title,
        string body,
        Dictionary<string, string>? data,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _messagingClient.SendMulticastAsync(
                tokens.Select(t => t.Token).ToList(),
                title,
                body,
                data,
                cancellationToken);

            if (response.Failures.Count == 0) return false;

            var tokenByValue = tokens.ToDictionary(t => t.Token);
            foreach (var group in response.Failures.GroupBy(f => f.ErrorCode))
            {
                _logger.LogWarning(
                    "FCM failed {FailureCount}/{Total} localized messages for {Category} locale {Locale} with {ErrorCode}",
                    group.Count(),
                    tokens.Count,
                    category,
                    locale,
                    group.Key);
            }

            var removedAny = false;
            foreach (var failure in response.Failures.Where(IsInvalidTokenFailure))
            {
                if (!tokenByValue.TryGetValue(failure.Token, out var token)) continue;

                _deviceTokenRepository.Remove(token);
                removedAny = true;
            }

            return removedAny;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send localized FCM notifications for {Category} locale {Locale}",
                category,
                locale);
            return false;
        }
    }
}
