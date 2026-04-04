namespace Convy.Application.Features.Items.DTOs;

public record DuplicateCheckDto(
    bool HasPotentialDuplicates,
    IReadOnlyList<DuplicateItemDto> PotentialDuplicates);

public record DuplicateItemDto(
    Guid Id,
    string Title,
    int? Quantity,
    string? Unit);
