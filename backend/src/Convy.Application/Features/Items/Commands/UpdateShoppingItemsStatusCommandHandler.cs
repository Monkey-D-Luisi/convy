using Convy.Application.Common.Interfaces;
using Convy.Application.Common.Models;
using Convy.Application.Features.Items.DTOs;
using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using Convy.Domain.ValueObjects;
using MediatR;

namespace Convy.Application.Features.Items.Commands;

public class UpdateShoppingItemsStatusCommandHandler : IRequestHandler<UpdateShoppingItemsStatusCommand, Result<SmartStatusBatchResult>>
{
    private readonly IListItemRepository _itemRepository;
    private readonly IHouseholdListRepository _listRepository;
    private readonly IHouseholdRepository _householdRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IHouseholdNotificationService _notifications;
    private readonly IActivityLogger _activityLogger;

    public UpdateShoppingItemsStatusCommandHandler(
        IListItemRepository itemRepository,
        IHouseholdListRepository listRepository,
        IHouseholdRepository householdRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUser,
        IHouseholdNotificationService notifications,
        IActivityLogger activityLogger)
    {
        _itemRepository = itemRepository;
        _listRepository = listRepository;
        _householdRepository = householdRepository;
        _userRepository = userRepository;
        _currentUser = currentUser;
        _notifications = notifications;
        _activityLogger = activityLogger;
    }

    public async Task<Result<SmartStatusBatchResult>> Handle(UpdateShoppingItemsStatusCommand request, CancellationToken cancellationToken)
    {
        var list = await _listRepository.GetByIdAsync(request.ListId, cancellationToken);
        if (list is null)
            return Result<SmartStatusBatchResult>.Failure(Error.NotFound("List not found."));

        if (list.Type != ListType.Shopping)
            return Result<SmartStatusBatchResult>.Failure(Error.Validation("Items are only supported for shopping lists."));

        var household = await _householdRepository.GetByIdWithMembersAsync(list.HouseholdId, cancellationToken);
        if (household is null || !household.IsMember(_currentUser.UserId))
            return Result<SmartStatusBatchResult>.Failure(Error.Forbidden("You are not a member of this household."));

        var completed = new List<SmartMatchedItemDto>();
        var uncompleted = new List<SmartMatchedItemDto>();
        var unchanged = new List<SmartMatchedItemDto>();
        var rejected = new List<SmartRejectedInputDto>();
        var warnings = new List<SmartWarningDto>();
        var changed = new List<ListItem>();
        var seen = new HashSet<Guid>();

        foreach (var itemId in request.ItemIds)
        {
            if (!seen.Add(itemId))
            {
                rejected.Add(new SmartRejectedInputDto(itemId.ToString(), "duplicate_in_request"));
                continue;
            }

            var item = await _itemRepository.GetByIdAsync(itemId, cancellationToken);
            if (item is null || item.ListId != request.ListId)
            {
                rejected.Add(new SmartRejectedInputDto(itemId.ToString(), "not_found"));
                continue;
            }

            if (request.Status == SmartItemStatus.Completed)
            {
                if (item.IsCompleted)
                {
                    unchanged.Add(new SmartMatchedItemDto(item.Id, item.Title, "already_completed"));
                    continue;
                }

                item.Complete(_currentUser.UserId);
                changed.Add(item);
                completed.Add(new SmartMatchedItemDto(item.Id, item.Title, "completed"));
            }
            else
            {
                if (!item.IsCompleted)
                {
                    unchanged.Add(new SmartMatchedItemDto(item.Id, item.Title, "already_pending"));
                    continue;
                }

                item.Uncomplete(_currentUser.UserId);
                changed.Add(item);
                uncompleted.Add(new SmartMatchedItemDto(item.Id, item.Title, "pending"));
            }
        }

        if (changed.Count > 0)
            await _itemRepository.SaveChangesAsync(cancellationToken);

        var user = await _userRepository.GetByIdAsync(_currentUser.UserId, cancellationToken);
        var userName = user?.DisplayName ?? "Unknown";
        foreach (var item in changed)
        {
            var dto = ToDto(item, userName);
            if (item.IsCompleted)
            {
                await _notifications.NotifyItemCompleted(list.HouseholdId, dto, cancellationToken);
                await _activityLogger.LogAsync(list.HouseholdId, ActivityEntityType.Item, item.Id, ActivityActionType.Completed, _currentUser.UserId, item.Title, cancellationToken);
            }
            else
            {
                await _notifications.NotifyItemUncompleted(list.HouseholdId, dto, cancellationToken);
                await _activityLogger.LogAsync(list.HouseholdId, ActivityEntityType.Item, item.Id, ActivityActionType.Uncompleted, _currentUser.UserId, item.Title, cancellationToken);
            }
        }

        return Result<SmartStatusBatchResult>.Success(new SmartStatusBatchResult(completed, uncompleted, unchanged, rejected, warnings));
    }

    private static ListItemDto ToDto(ListItem item, string userName) =>
        new(item.Id, item.Title, item.Quantity, item.Unit, item.Note,
            item.ListId, item.CreatedBy, userName, item.CreatedAt,
            item.IsCompleted, item.CompletedBy, userName, item.CompletedAt,
            item.ReturnedToPendingBy, userName, item.ReturnedToPendingAt,
            item.RecurrenceFrequency?.ToString(), item.RecurrenceInterval, item.NextDueDate);
}
