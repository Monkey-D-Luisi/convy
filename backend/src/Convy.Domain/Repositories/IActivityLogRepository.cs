using Convy.Domain.Entities;

namespace Convy.Domain.Repositories;

public interface IActivityLogRepository
{
    Task<IReadOnlyList<ActivityLog>> GetByHouseholdIdAsync(Guid householdId, int limit = 50, DateTime? before = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ActivityLog>> GetByEntityIdAsync(Guid entityId, int limit = 50, CancellationToken cancellationToken = default);
    Task AddAsync(ActivityLog log, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
