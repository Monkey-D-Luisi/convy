using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using Convy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Convy.Infrastructure.Repositories;

public class TaskItemRepository : ITaskItemRepository
{
    private readonly ConvyDbContext _context;

    public TaskItemRepository(ConvyDbContext context)
    {
        _context = context;
    }

    public async Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.TaskItems.FindAsync([id], cancellationToken);
    }

    public async Task<IReadOnlyList<TaskItem>> GetByListIdAsync(
        Guid listId,
        string? status = null,
        Guid? createdBy = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.TaskItems.Where(t => t.ListId == listId);

        if (status is "Pending")
            query = query.Where(t => !t.IsCompleted);
        else if (status is "Completed")
            query = query.Where(t => t.IsCompleted);

        if (createdBy.HasValue)
            query = query.Where(t => t.CreatedBy == createdBy.Value);

        if (fromDate.HasValue)
            query = query.Where(t => t.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(t => t.CreatedAt <= toDate.Value);

        return await query
            .OrderByDescending(t => t.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        await _context.TaskItems.AddAsync(task, cancellationToken);
    }

    public void Remove(TaskItem task)
    {
        _context.TaskItems.Remove(task);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
