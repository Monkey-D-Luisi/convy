using Convy.Application.Common.Models;
using Convy.Domain.ValueObjects;
using MediatR;

namespace Convy.Application.Features.Items.Commands;

public record SmartBatchCreateItemsCommand(
    Guid ListId,
    IReadOnlyList<SmartShoppingItemInput> Items,
    ItemCreationSource Source = ItemCreationSource.Manual) : IRequest<Result<SmartBatchCreateItemsResult>>;

public record SmartShoppingItemInput(string Title, int? Quantity, string? Unit, string? Note);

public record SmartBatchCreateItemsResult(
    IReadOnlyList<SmartCreatedItemDto> Created,
    IReadOnlyList<SmartMatchedItemDto> Reused,
    IReadOnlyList<SmartMatchedItemDto> Uncompleted,
    IReadOnlyList<SmartMatchedItemDto> Unchanged,
    IReadOnlyList<SmartRejectedInputDto> Rejected,
    IReadOnlyList<SmartWarningDto> Warnings);

public record SmartCreatedItemDto(Guid Id, string Title, int? Quantity, string? Unit, string? Note, ItemCreationSource Source);
public record SmartMatchedItemDto(Guid Id, string Title, string Reason);
public record SmartRejectedInputDto(string Title, string Reason);
public record SmartWarningDto(string Title, string Reason, string Message);
