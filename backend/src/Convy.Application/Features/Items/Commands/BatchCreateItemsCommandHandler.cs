using Convy.Application.Common.Interfaces;
using Convy.Application.Common.Services;
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
    private readonly IUserFacingTextNormalizer _textNormalizer;

    public BatchCreateItemsCommandHandler(
        IListItemRepository itemRepository,
        IHouseholdListRepository listRepository,
        IHouseholdRepository householdRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUser,
        IHouseholdNotificationService notifications,
        IActivityLogger activityLogger,
        IUserFacingTextNormalizer? textNormalizer = null)
    {
        _itemRepository = itemRepository;
        _listRepository = listRepository;
        _householdRepository = householdRepository;
        _userRepository = userRepository;
        _currentUser = currentUser;
        _notifications = notifications;
        _activityLogger = activityLogger;
        _textNormalizer = textNormalizer ?? new UserFacingTextNormalizer();
    }

    public async Task<Result<BatchCreateResult>> Handle(BatchCreateItemsCommand request, CancellationToken cancellationToken)
    {
        var list = await _listRepository.GetByIdAsync(request.ListId, cancellationToken);
        if (list is null)
            return Result<BatchCreateResult>.Failure(Error.NotFound("List not found."));

        if (list.Type != ListType.Shopping)
            return Result<BatchCreateResult>.Failure(Error.Validation("Items are only supported for shopping lists."));

        var household = await _householdRepository.GetByIdWithMembersAsync(list.HouseholdId, cancellationToken);
        if (household is null || !household.IsMember(_currentUser.UserId))
            return Result<BatchCreateResult>.Failure(Error.Forbidden("You are not a member of this household."));

        var existingItems = await _itemRepository.GetByListIdAsync(request.ListId, "All", null, null, null, cancellationToken)
            ?? Array.Empty<ListItem>();
        var existingByTitle = existingItems
            .GroupBy(GetNormalizedTitle, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.OrderBy(item => item.IsCompleted).First(), StringComparer.Ordinal);

        var items = new List<ListItem>();
        var uncompletedItems = new List<ListItem>();
        var seenInRequest = new HashSet<string>(StringComparer.Ordinal);
        foreach (var dto in request.Items)
        {
            var title = _textNormalizer.NormalizeTitle(dto.Title);
            var normalizedTitle = _textNormalizer.NormalizeForComparison(title);
            if (!seenInRequest.Add(normalizedTitle))
                continue;

            if (existingByTitle.TryGetValue(normalizedTitle, out var existing))
            {
                if (existing.IsCompleted)
                {
                    var tracked = await _itemRepository.GetByIdAsync(existing.Id, cancellationToken);
                    if (tracked is not null && tracked.IsCompleted)
                    {
                        tracked.Uncomplete(_currentUser.UserId);
                        uncompletedItems.Add(tracked);
                    }
                }

                continue;
            }

            var item = new ListItem(title, normalizedTitle, request.ListId, _currentUser.UserId, dto.Quantity, dto.Unit, dto.Note, request.Source);
            await _itemRepository.AddAsync(item, cancellationToken);
            items.Add(item);
        }

        if (items.Count > 0 || uncompletedItems.Count > 0)
            await _itemRepository.SaveChangesAsync(cancellationToken);

        var user = await _userRepository.GetByIdAsync(_currentUser.UserId, cancellationToken);
        var userName = user?.DisplayName ?? "Unknown";

        foreach (var item in items)
        {
            var itemDto = new ListItemDto(item.Id, item.Title, item.Quantity, item.Unit, item.Note,
                item.ListId, item.CreatedBy, userName, item.CreatedAt,
                item.IsCompleted, item.CompletedBy, null, item.CompletedAt,
                item.ReturnedToPendingBy, null, item.ReturnedToPendingAt,
                item.RecurrenceFrequency?.ToString(), item.RecurrenceInterval, item.NextDueDate);
            await _notifications.NotifyItemCreated(list.HouseholdId, itemDto, cancellationToken);
            await _activityLogger.LogAsync(list.HouseholdId, ActivityEntityType.Item, item.Id, ActivityActionType.Created, _currentUser.UserId, item.Title, cancellationToken);
        }

        foreach (var item in uncompletedItems)
        {
            var itemDto = ToDto(item, userName);
            await _notifications.NotifyItemUncompleted(list.HouseholdId, itemDto, cancellationToken);
            await _activityLogger.LogAsync(list.HouseholdId, ActivityEntityType.Item, item.Id, ActivityActionType.Uncompleted, _currentUser.UserId, item.Title, cancellationToken);
        }

        return Result<BatchCreateResult>.Success(new BatchCreateResult(items.Select(i => i.Id).ToList()));
    }

    private string GetNormalizedTitle(ListItem item) =>
        string.IsNullOrWhiteSpace(item.NormalizedTitle)
            ? _textNormalizer.NormalizeForComparison(item.Title)
            : item.NormalizedTitle;

    private static ListItemDto ToDto(ListItem item, string userName) =>
        new(item.Id, item.Title, item.Quantity, item.Unit, item.Note,
            item.ListId, item.CreatedBy, userName, item.CreatedAt,
            item.IsCompleted, item.CompletedBy, userName, item.CompletedAt,
            item.ReturnedToPendingBy, userName, item.ReturnedToPendingAt,
            item.RecurrenceFrequency?.ToString(), item.RecurrenceInterval, item.NextDueDate);
}
