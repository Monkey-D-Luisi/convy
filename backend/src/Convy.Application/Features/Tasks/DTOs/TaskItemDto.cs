using Convy.Domain.ValueObjects;

namespace Convy.Application.Features.Tasks.DTOs;

public record TaskItemDto(
    Guid Id,
    string Title,
    string? Note,
    Guid ListId,
    Guid CreatedBy,
    string CreatedByName,
    Guid? AssignedToUserId,
    string? AssignedToUserName,
    DateOnly? DueDate,
    DateTime? ReminderAtUtc,
    DateTime? ReminderSentAtUtc,
    TaskPriority Priority,
    DateTime CreatedAt,
    bool IsCompleted,
    Guid? CompletedBy,
    string? CompletedByName,
    DateTime? CompletedAt);
