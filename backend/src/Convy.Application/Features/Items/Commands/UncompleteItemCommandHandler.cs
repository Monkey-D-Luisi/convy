using Convy.Application.Common.Interfaces;
using Convy.Application.Common.Models;
using Convy.Application.Features.Items.DTOs;
using Convy.Domain.Repositories;
using Convy.Domain.ValueObjects;
using MediatR;

namespace Convy.Application.Features.Items.Commands;

public class UncompleteItemCommandHandler : IRequestHandler<UncompleteItemCommand, Result>
{
    private readonly IListItemRepository _itemRepository;
    private readonly IHouseholdListRepository _listRepository;
    private readonly IHouseholdRepository _householdRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IHouseholdNotificationService _notifications;
    private readonly IActivityLogger _activityLogger;

    public UncompleteItemCommandHandler(
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

    public async Task<Result> Handle(UncompleteItemCommand request, CancellationToken cancellationToken)
    {
        var item = await _itemRepository.GetByIdAsync(request.ItemId, cancellationToken);
        if (item is null)
            return Result.Failure(Error.NotFound("Item not found."));

        if (item.ListId != request.ListId)
            return Result.Failure(Error.NotFound("Item not found."));

        var list = await _listRepository.GetByIdAsync(item.ListId, cancellationToken);
        if (list is null)
            return Result.Failure(Error.NotFound("List not found."));

        if (list.Type != ListType.Shopping)
            return Result.Failure(Error.Validation("Items are only supported for shopping lists."));

        var household = await _householdRepository.GetByIdWithMembersAsync(list.HouseholdId, cancellationToken);
        if (household is null || !household.IsMember(_currentUser.UserId))
            return Result.Failure(Error.Forbidden("You are not a member of this household."));

        item.Uncomplete();

        await _itemRepository.SaveChangesAsync(cancellationToken);

        var user = await _userRepository.GetByIdAsync(item.CreatedBy, cancellationToken);
        var dto = new ListItemDto(item.Id, item.Title, item.Quantity, item.Unit, item.Note,
            item.ListId, item.CreatedBy, user?.DisplayName ?? "Unknown", item.CreatedAt,
            item.IsCompleted, item.CompletedBy, null, item.CompletedAt,
            item.RecurrenceFrequency?.ToString(), item.RecurrenceInterval, item.NextDueDate);
        await _notifications.NotifyItemUncompleted(list.HouseholdId, dto, cancellationToken);
        await _activityLogger.LogAsync(list.HouseholdId, ActivityEntityType.Item, item.Id, ActivityActionType.Uncompleted, _currentUser.UserId, item.Title, cancellationToken);

        return Result.Success();
    }
}
