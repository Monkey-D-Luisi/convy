using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using Convy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Convy.Infrastructure.Repositories;

public class HouseholdListRepository : IHouseholdListRepository
{
    private readonly ConvyDbContext _context;

    public HouseholdListRepository(ConvyDbContext context)
    {
        _context = context;
    }

    public async Task<HouseholdList?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.HouseholdLists.FindAsync([id], cancellationToken);
    }

    public async Task<IReadOnlyList<HouseholdList>> GetByHouseholdIdAsync(Guid householdId, bool includeArchived = false, CancellationToken cancellationToken = default)
    {
        var query = _context.HouseholdLists
            .Where(l => l.HouseholdId == householdId);

        if (!includeArchived)
            query = query.Where(l => !l.IsArchived);

        return await query
            .OrderByDescending(l => l.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(HouseholdList list, CancellationToken cancellationToken = default)
    {
        await _context.HouseholdLists.AddAsync(list, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
