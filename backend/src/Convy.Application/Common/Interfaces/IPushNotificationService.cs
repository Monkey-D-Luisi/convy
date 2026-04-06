namespace Convy.Application.Common.Interfaces;

public interface IPushNotificationService
{
    Task SendToUsersAsync(IEnumerable<Guid> userIds, string title, string body, Dictionary<string, string>? data = null, CancellationToken cancellationToken = default);
}
