using Convy.Application.Common.Interfaces;
using Convy.Application.Common.Models;
using Convy.Application.Features.Items.DTOs;
using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using Convy.Domain.ValueObjects;
using MediatR;

namespace Convy.Application.Features.Items.Commands;

public class SmartBatchCreateItemsCommandHandler : IRequestHandler<SmartBatchCreateItemsCommand, Result<SmartBatchCreateItemsResult>>
{
    private readonly IListItemRepository _itemRepository;
    private readonly IHouseholdListRepository _listRepository;
    private readonly IHouseholdRepository _householdRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IHouseholdNotificationService _notifications;
    private readonly IActivityLogger _activityLogger;
    private readonly IUserFacingTextNormalizer _textNormalizer;

    public SmartBatchCreateItemsCommandHandler(
        IListItemRepository itemRepository,
        IHouseholdListRepository listRepository,
        IHouseholdRepository householdRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUser,
        IHouseholdNotificationService notifications,
        IActivityLogger activityLogger,
        IUserFacingTextNormalizer textNormalizer)
    {
        _itemRepository = itemRepository;
        _listRepository = listRepository;
        _householdRepository = householdRepository;
        _userRepository = userRepository;
        _currentUser = currentUser;
        _notifications = notifications;
        _activityLogger = activityLogger;
        _textNormalizer = textNormalizer;
    }

    public async Task<Result<SmartBatchCreateItemsResult>> Handle(SmartBatchCreateItemsCommand request, CancellationToken cancellationToken)
    {
        var list = await _listRepository.GetByIdAsync(request.ListId, cancellationToken);
        if (list is null)
            return Result<SmartBatchCreateItemsResult>.Failure(Error.NotFound("List not found."));

        if (list.Type != ListType.Shopping)
            return Result<SmartBatchCreateItemsResult>.Failure(Error.Validation("Items are only supported for shopping lists."));

        var household = await _householdRepository.GetByIdWithMembersAsync(list.HouseholdId, cancellationToken);
        if (household is null || !household.IsMember(_currentUser.UserId))
            return Result<SmartBatchCreateItemsResult>.Failure(Error.Forbidden("You are not a member of this household."));

        var existingItems = await _itemRepository.GetByListIdAsync(request.ListId, "All", null, null, null, cancellationToken);
        var existingByTitle = existingItems
            .GroupBy(GetNormalizedTitle, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.OrderBy(item => item.IsCompleted).First(), StringComparer.Ordinal);

        var created = new List<SmartCreatedItemDto>();
        var reused = new List<SmartMatchedItemDto>();
        var uncompleted = new List<SmartMatchedItemDto>();
        var unchanged = new List<SmartMatchedItemDto>();
        var rejected = new List<SmartRejectedInputDto>();
        var warnings = new List<SmartWarningDto>();
        var seenInRequest = new Dictionary<string, SmartMatchedItemDto>(StringComparer.Ordinal);
        var createdEntities = new List<ListItem>();
        var uncompletedEntities = new List<ListItem>();

        foreach (var input in request.Items)
        {
            var title = _textNormalizer.NormalizeTitle(input.Title);
            var normalizedTitle = _textNormalizer.NormalizeForComparison(title);
            if (normalizedTitle.Length == 0)
            {
                rejected.Add(new SmartRejectedInputDto(input.Title, "empty_after_normalization"));
                continue;
            }

            if (seenInRequest.TryGetValue(normalizedTitle, out var duplicate))
            {
                reused.Add(duplicate with { Reason = "duplicate_in_request" });
                continue;
            }

            if (existingByTitle.TryGetValue(normalizedTitle, out var existing))
            {
                var match = new SmartMatchedItemDto(existing.Id, existing.Title, existing.IsCompleted ? "was_completed" : "already_pending");
                seenInRequest[normalizedTitle] = match;

                if (HasDetailConflict(existing, input))
                {
                    reused.Add(match);
                    warnings.Add(new SmartWarningDto(existing.Title, "quantity_conflict", $"{existing.Title} already exists with different details. It was not changed."));
                    continue;
                }

                if (existing.IsCompleted)
                {
                    var tracked = await _itemRepository.GetByIdAsync(existing.Id, cancellationToken);
                    if (tracked is not null)
                    {
                        tracked.Uncomplete(_currentUser.UserId);
                        uncompletedEntities.Add(tracked);
                    }

                    uncompleted.Add(match);
                }
                else
                {
                    reused.Add(match);
                }

                continue;
            }

            var item = new ListItem(title, normalizedTitle, request.ListId, _currentUser.UserId, input.Quantity, input.Unit, input.Note, request.Source);
            await _itemRepository.AddAsync(item, cancellationToken);
            createdEntities.Add(item);
            var createdDto = new SmartCreatedItemDto(item.Id, item.Title, item.Quantity, item.Unit, item.Note, item.Source);
            created.Add(createdDto);
            seenInRequest[normalizedTitle] = new SmartMatchedItemDto(item.Id, item.Title, "created_in_request");
        }

        if (createdEntities.Count > 0 || uncompletedEntities.Count > 0)
            await _itemRepository.SaveChangesAsync(cancellationToken);

        var user = await _userRepository.GetByIdAsync(_currentUser.UserId, cancellationToken);
        var userName = user?.DisplayName ?? "Unknown";
        foreach (var item in createdEntities)
        {
            await _notifications.NotifyItemCreated(list.HouseholdId, ToDto(item, userName), cancellationToken);
            await _activityLogger.LogAsync(list.HouseholdId, ActivityEntityType.Item, item.Id, ActivityActionType.Created, _currentUser.UserId, item.Title, cancellationToken);
        }

        foreach (var item in uncompletedEntities)
        {
            await _notifications.NotifyItemUncompleted(list.HouseholdId, ToDto(item, userName), cancellationToken);
            await _activityLogger.LogAsync(list.HouseholdId, ActivityEntityType.Item, item.Id, ActivityActionType.Uncompleted, _currentUser.UserId, item.Title, cancellationToken);
        }

        return Result<SmartBatchCreateItemsResult>.Success(new SmartBatchCreateItemsResult(created, reused, uncompleted, unchanged, rejected, warnings));
    }

    private static bool HasDetailConflict(ListItem existing, SmartShoppingItemInput input)
    {
        if (input.Quantity.HasValue && existing.Quantity != input.Quantity)
            return true;

        if (!string.IsNullOrWhiteSpace(input.Unit) && !string.Equals(existing.Unit, input.Unit.Trim(), StringComparison.OrdinalIgnoreCase))
            return true;

        if (!string.IsNullOrWhiteSpace(input.Note) && !string.Equals(existing.Note, input.Note.Trim(), StringComparison.Ordinal))
            return true;

        return false;
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
