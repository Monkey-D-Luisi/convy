using Convy.Application.Common.Models;

namespace Convy.Application.Common.Interfaces;

public interface IPushNotificationService
{
    Task SendLocalizedAsync(
        IEnumerable<Guid> userIds,
        NotificationCategory category,
        PushNotificationTemplate template,
        Dictionary<string, string>? data = null,
        CancellationToken cancellationToken = default);
}
