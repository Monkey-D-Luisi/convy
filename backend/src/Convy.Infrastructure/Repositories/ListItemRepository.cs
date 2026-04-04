using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using Convy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Convy.Infrastructure.Repositories;

public class ListItemRepository : IListItemRepository
{
    private readonly ConvyDbContext _context;

    public ListItemRepository(ConvyDbContext context)
    {
        _context = context;
    }

    public async Task<ListItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ListItems.FindAsync([id], cancellationToken);
    }

    public async Task<IReadOnlyList<ListItem>> GetByListIdAsync(Guid listId, bool includeCompleted = true, CancellationToken cancellationToken = default)
    {
        var query = _context.ListItems
            .Where(i => i.ListId == listId);

        if (!includeCompleted)
            query = query.Where(i => !i.IsCompleted);

        return await query
            .OrderByDescending(i => i.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ListItem item, CancellationToken cancellationToken = default)
    {
        await _context.ListItems.AddAsync(item, cancellationToken);
    }

    public void Remove(ListItem item)
    {
        _context.ListItems.Remove(item);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
