using Convy.Domain.Entities;

namespace Convy.Domain.Repositories;

public interface IHouseholdListRepository
{
    Task<HouseholdList?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HouseholdList>> GetByHouseholdIdAsync(Guid householdId, bool includeArchived = false, CancellationToken cancellationToken = default);
    Task AddAsync(HouseholdList list, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
