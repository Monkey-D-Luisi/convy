namespace Convy.Application.Features.Tasks.DTOs;

public record TaskItemDto(
    Guid Id,
    string Title,
    string? Note,
    Guid ListId,
    Guid CreatedBy,
    string CreatedByName,
    DateTime CreatedAt,
    bool IsCompleted,
    Guid? CompletedBy,
    string? CompletedByName,
    DateTime? CompletedAt);
