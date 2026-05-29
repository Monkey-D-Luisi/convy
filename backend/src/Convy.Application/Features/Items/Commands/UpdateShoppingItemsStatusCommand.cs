using Convy.Application.Common.Models;
using MediatR;

namespace Convy.Application.Features.Items.Commands;

public record UpdateShoppingItemsStatusCommand(
    Guid ListId,
    IReadOnlyList<Guid> ItemIds,
    SmartItemStatus Status) : IRequest<Result<SmartStatusBatchResult>>;

public enum SmartItemStatus
{
    Pending = 0,
    Completed = 1,
}

public record SmartStatusBatchResult(
    IReadOnlyList<SmartMatchedItemDto> Completed,
    IReadOnlyList<SmartMatchedItemDto> Uncompleted,
    IReadOnlyList<SmartMatchedItemDto> Unchanged,
    IReadOnlyList<SmartRejectedInputDto> Rejected,
    IReadOnlyList<SmartWarningDto> Warnings);
