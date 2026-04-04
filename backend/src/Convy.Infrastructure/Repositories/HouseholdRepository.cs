using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using Convy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Convy.Infrastructure.Repositories;

public class HouseholdRepository : IHouseholdRepository
{
    private readonly ConvyDbContext _context;

    public HouseholdRepository(ConvyDbContext context)
    {
        _context = context;
    }

    public async Task<Household?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Households.FindAsync([id], cancellationToken);
    }

    public async Task<Household?> GetByIdWithMembersAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Households
            .Include(h => h.Memberships)
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Household>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Households
            .Where(h => h.Memberships.Any(m => m.UserId == userId))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Household household, CancellationToken cancellationToken = default)
    {
        await _context.Households.AddAsync(household, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
