namespace Convy.Application.Features.Invites.DTOs;

public record InviteDto(
    Guid Id,
    Guid HouseholdId,
    string Code,
    DateTime ExpiresAt,
    bool IsValid,
    DateTime CreatedAt);
