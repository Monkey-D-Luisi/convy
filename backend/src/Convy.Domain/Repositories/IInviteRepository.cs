using Convy.Domain.Entities;

namespace Convy.Domain.Repositories;

public interface IInviteRepository
{
    Task<Invite?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Invite?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<List<Invite>> GetByHouseholdIdAsync(Guid householdId, CancellationToken cancellationToken = default);
    Task AddAsync(Invite invite, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
