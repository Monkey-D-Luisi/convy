using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using Convy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Convy.Infrastructure.Repositories;

public class InviteRepository : IInviteRepository
{
    private readonly ConvyDbContext _context;

    public InviteRepository(ConvyDbContext context)
    {
        _context = context;
    }

    public async Task<Invite?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Invites.FindAsync([id], cancellationToken);
    }

    public async Task<Invite?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _context.Invites
            .FirstOrDefaultAsync(i => i.Code == code, cancellationToken);
    }

    public async Task<List<Invite>> GetByHouseholdIdAsync(Guid householdId, CancellationToken cancellationToken = default)
    {
        return await _context.Invites
            .Where(i => i.HouseholdId == householdId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Invite invite, CancellationToken cancellationToken = default)
    {
        await _context.Invites.AddAsync(invite, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
