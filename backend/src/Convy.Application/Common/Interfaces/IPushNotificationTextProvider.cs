using Convy.Application.Common.Models;

namespace Convy.Application.Common.Interfaces;

public interface IPushNotificationTextProvider
{
    LocalizedPushNotification Render(PushNotificationTemplate template, string? locale);
}
