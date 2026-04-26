using Convy.Domain.Entities;

namespace Convy.Domain.Repositories;

public interface INotificationPreferencesRepository
{
    Task<NotificationPreferences?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationPreferences>> GetByUserIdsAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default);
    Task AddAsync(NotificationPreferences preferences, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
