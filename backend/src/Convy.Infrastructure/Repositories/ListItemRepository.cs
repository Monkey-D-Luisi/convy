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

    public async Task<IReadOnlyList<ListItem>> GetByListIdAsync(
        Guid listId,
        string? status = null,
        Guid? createdBy = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ListItems.Where(i => i.ListId == listId);

        if (status is "Pending")
            query = query.Where(i => !i.IsCompleted);
        else if (status is "Completed")
            query = query.Where(i => i.IsCompleted);

        if (createdBy.HasValue)
            query = query.Where(i => i.CreatedBy == createdBy.Value);

        if (fromDate.HasValue)
            query = query.Where(i => i.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(i => i.CreatedAt <= toDate.Value);

        return await query
            .OrderByDescending(i => i.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ListItem>> SearchByTitleInListAsync(Guid listId, string title, CancellationToken cancellationToken = default)
    {
        var normalizedTitle = title.Trim().ToLowerInvariant();

        return await _context.ListItems
            .Where(i => i.ListId == listId && !i.IsCompleted)
            .Where(i => EF.Functions.ILike(i.Title, $"%{normalizedTitle}%"))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetFrequentTitlesAsync(Guid householdId, string? query, int limit = 10, CancellationToken cancellationToken = default)
    {
        var householdListIds = _context.HouseholdLists
            .Where(l => l.HouseholdId == householdId)
            .Select(l => l.Id);

        var titlesQuery = _context.ListItems
            .Where(i => householdListIds.Contains(i.ListId));

        if (!string.IsNullOrWhiteSpace(query))
        {
            var normalizedQuery = query.Trim().ToLowerInvariant();
            titlesQuery = titlesQuery.Where(i => EF.Functions.ILike(i.Title, $"%{normalizedQuery}%"));
        }

        return await titlesQuery
            .GroupBy(i => i.Title.ToLower())
            .OrderByDescending(g => g.Count())
            .Take(limit)
            .Select(g => g.First().Title)
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

    public async Task<List<ListItem>> GetDueRecurringItemsAsync(DateTime asOf, CancellationToken cancellationToken = default)
    {
        return await _context.ListItems
            .Where(i => i.NextDueDate != null && i.NextDueDate <= asOf && i.IsCompleted)
            .ToListAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
