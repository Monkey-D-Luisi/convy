namespace Convy.Application.Features.Users.DTOs;

public record UserDto(
    Guid Id,
    string DisplayName,
    string Email,
    DateTime CreatedAt);
