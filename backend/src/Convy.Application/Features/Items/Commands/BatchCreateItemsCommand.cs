using Convy.Application.Common.Models;
using MediatR;

namespace Convy.Application.Features.Items.Commands;

public record BatchCreateItemsCommand(
    Guid ListId,
    List<BatchItemDto> Items) : IRequest<Result<BatchCreateResult>>;

public record BatchItemDto(string Title, int? Quantity, string? Unit, string? Note);

public record BatchCreateResult(List<Guid> CreatedIds);
