using Convy.Application.Common.Interfaces;
using Convy.Application.Common.Models;
using Convy.Application.Features.Items.DTOs;
using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using Convy.Domain.ValueObjects;
using MediatR;

namespace Convy.Application.Features.Items.Commands;

public class BatchCreateItemsCommandHandler : IRequestHandler<BatchCreateItemsCommand, Result<BatchCreateResult>>
{
    private readonly IListItemRepository _itemRepository;
    private readonly IHouseholdListRepository _listRepository;
    private readonly IHouseholdRepository _householdRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IHouseholdNotificationService _notifications;
    private readonly IActivityLogger _activityLogger;

    public BatchCreateItemsCommandHandler(
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

    public async Task<Result<BatchCreateResult>> Handle(BatchCreateItemsCommand request, CancellationToken cancellationToken)
    {
        var list = await _listRepository.GetByIdAsync(request.ListId, cancellationToken);
        if (list is null)
            return Result<BatchCreateResult>.Failure(Error.NotFound("List not found."));

        var household = await _householdRepository.GetByIdWithMembersAsync(list.HouseholdId, cancellationToken);
        if (household is null || !household.IsMember(_currentUser.UserId))
            return Result<BatchCreateResult>.Failure(Error.Forbidden("You are not a member of this household."));

        var items = new List<ListItem>();
        foreach (var dto in request.Items)
        {
            var item = new ListItem(dto.Title, request.ListId, _currentUser.UserId, dto.Quantity, dto.Unit, dto.Note);
            await _itemRepository.AddAsync(item, cancellationToken);
            items.Add(item);
        }

        await _itemRepository.SaveChangesAsync(cancellationToken);

        var user = await _userRepository.GetByIdAsync(_currentUser.UserId, cancellationToken);
        var userName = user?.DisplayName ?? "Unknown";

        foreach (var item in items)
        {
            var itemDto = new ListItemDto(item.Id, item.Title, item.Quantity, item.Unit, item.Note,
                item.ListId, item.CreatedBy, userName, item.CreatedAt,
                item.IsCompleted, item.CompletedBy, null, item.CompletedAt,
                item.RecurrenceFrequency?.ToString(), item.RecurrenceInterval, item.NextDueDate);
            await _notifications.NotifyItemCreated(list.HouseholdId, itemDto, cancellationToken);
            await _activityLogger.LogAsync(list.HouseholdId, ActivityEntityType.Item, item.Id, ActivityActionType.Created, _currentUser.UserId, item.Title, cancellationToken);
        }

        return Result<BatchCreateResult>.Success(new BatchCreateResult(items.Select(i => i.Id).ToList()));
    }
}
