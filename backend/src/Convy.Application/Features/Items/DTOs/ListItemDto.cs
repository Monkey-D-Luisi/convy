namespace Convy.Application.Features.Items.DTOs;

public record ListItemDto(
    Guid Id,
    string Title,
    int? Quantity,
    string? Unit,
    string? Note,
    Guid ListId,
    Guid CreatedBy,
    string CreatedByName,
    DateTime CreatedAt,
    bool IsCompleted,
    Guid? CompletedBy,
    string? CompletedByName,
    DateTime? CompletedAt,
    string? RecurrenceFrequency,
    int? RecurrenceInterval,
    DateTime? NextDueDate);
