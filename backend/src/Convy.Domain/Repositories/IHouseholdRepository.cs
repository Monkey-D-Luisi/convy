using Convy.Domain.Entities;

namespace Convy.Domain.Repositories;

public interface IHouseholdRepository
{
    Task<Household?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Household?> GetByIdWithMembersAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Household>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(Household household, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
