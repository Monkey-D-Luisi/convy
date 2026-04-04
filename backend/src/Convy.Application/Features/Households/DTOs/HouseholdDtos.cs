using Convy.Domain.ValueObjects;

namespace Convy.Application.Features.Households.DTOs;

public record HouseholdDto(
    Guid Id,
    string Name,
    Guid CreatedBy,
    DateTime CreatedAt);

public record HouseholdMemberDto(
    Guid UserId,
    string DisplayName,
    string Email,
    HouseholdRole Role,
    DateTime JoinedAt);

public record HouseholdDetailDto(
    Guid Id,
    string Name,
    Guid CreatedBy,
    DateTime CreatedAt,
    IReadOnlyList<HouseholdMemberDto> Members);
