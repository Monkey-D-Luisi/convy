using Convy.Domain.Entities;

namespace Convy.Application.Features.Tasks.DTOs;

public static class TaskItemMapper
{
    public static TaskItemDto ToDto(TaskItem task, IReadOnlyDictionary<Guid, string> userNames)
    {
        return new TaskItemDto(
            task.Id,
            task.Title,
            task.Note,
            task.ListId,
            task.CreatedBy,
            userNames.GetValueOrDefault(task.CreatedBy, "Unknown"),
            task.CreatedAt,
            task.IsCompleted,
            task.CompletedBy,
            task.CompletedBy.HasValue ? userNames.GetValueOrDefault(task.CompletedBy.Value, "Unknown") : null,
            task.CompletedAt);
    }
}
