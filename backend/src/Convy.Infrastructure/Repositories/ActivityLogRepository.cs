using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using Convy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Convy.Infrastructure.Repositories;

public class ActivityLogRepository : IActivityLogRepository
{
    private readonly ConvyDbContext _context;

    public ActivityLogRepository(ConvyDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ActivityLog>> GetByHouseholdIdAsync(Guid householdId, int limit = 50, CancellationToken cancellationToken = default)
    {
        return await _context.ActivityLogs
            .Where(a => a.HouseholdId == householdId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ActivityLog log, CancellationToken cancellationToken = default)
    {
        await _context.ActivityLogs.AddAsync(log, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
