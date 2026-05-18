using Convy.Application.Common.Models;
using Convy.Domain.ValueObjects;
using MediatR;

namespace Convy.Application.Features.Items.Commands;

public record BatchCreateItemsCommand(
    Guid ListId,
    List<BatchItemDto> Items,
    ItemCreationSource Source = ItemCreationSource.Manual) : IRequest<Result<BatchCreateResult>>;

public record BatchItemDto(string Title, int? Quantity, string? Unit, string? Note);

public record BatchCreateResult(List<Guid> CreatedIds);
