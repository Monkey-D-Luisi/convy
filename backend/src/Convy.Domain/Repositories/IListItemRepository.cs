using Convy.Domain.Entities;

namespace Convy.Domain.Repositories;

public interface IListItemRepository
{
    Task<ListItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ListItem>> GetByListIdAsync(
        Guid listId,
        string? status = null,
        Guid? createdBy = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ListItem>> SearchByTitleInListAsync(Guid listId, string title, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetFrequentTitlesAsync(Guid householdId, string? query, int limit = 10, CancellationToken cancellationToken = default);
    Task AddAsync(ListItem item, CancellationToken cancellationToken = default);
    void Remove(ListItem item);
    Task<List<ListItem>> GetDueRecurringItemsAsync(DateTime asOf, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
