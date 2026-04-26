using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using Convy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Convy.Infrastructure.Repositories;

public class NotificationPreferencesRepository : INotificationPreferencesRepository
{
    private readonly ConvyDbContext _context;

    public NotificationPreferencesRepository(ConvyDbContext context)
    {
        _context = context;
    }

    public async Task<NotificationPreferences?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.NotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationPreferences>> GetByUserIdsAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default)
    {
        var userIdList = userIds.Distinct().ToList();
        return await _context.NotificationPreferences
            .AsNoTracking()
            .Where(p => userIdList.Contains(p.UserId))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(NotificationPreferences preferences, CancellationToken cancellationToken = default)
    {
        await _context.NotificationPreferences.AddAsync(preferences, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
