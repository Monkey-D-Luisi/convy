using Convy.Domain.ValueObjects;

namespace Convy.Application.Features.Lists.DTOs;

public record HouseholdListDto(
    Guid Id,
    string Name,
    ListType Type,
    Guid HouseholdId,
    Guid CreatedBy,
    DateTime CreatedAt,
    bool IsArchived,
    DateTime? ArchivedAt);
