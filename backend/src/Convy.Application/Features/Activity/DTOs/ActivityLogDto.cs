using Convy.Domain.ValueObjects;

namespace Convy.Application.Features.Activity.DTOs;

public record ActivityLogDto(
    Guid Id,
    Guid HouseholdId,
    ActivityEntityType EntityType,
    Guid EntityId,
    ActivityActionType ActionType,
    Guid PerformedBy,
    string PerformedByName,
    DateTime CreatedAt,
    string? Metadata);
