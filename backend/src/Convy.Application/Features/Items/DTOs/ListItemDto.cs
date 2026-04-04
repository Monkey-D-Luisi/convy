namespace Convy.Application.Features.Items.DTOs;

public record ListItemDto(
    Guid Id,
    string Title,
    int? Quantity,
    string? Unit,
    string? Note,
    Guid ListId,
    Guid CreatedBy,
    DateTime CreatedAt,
    bool IsCompleted,
    Guid? CompletedBy,
    DateTime? CompletedAt);
