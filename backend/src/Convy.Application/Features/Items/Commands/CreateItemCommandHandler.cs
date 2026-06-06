using Convy.Application.Common.Interfaces;
using Convy.Application.Common.Services;
using Convy.Application.Common.Models;
using Convy.Application.Features.Items.DTOs;
using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using Convy.Domain.ValueObjects;
using MediatR;

namespace Convy.Application.Features.Items.Commands;

public class CreateItemCommandHandler : IRequestHandler<CreateItemCommand, Result<Guid>>
{
    private readonly IListItemRepository _itemRepository;
    private readonly IHouseholdListRepository _listRepository;
    private readonly IHouseholdRepository _householdRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IHouseholdNotificationService _notifications;
    private readonly IActivityLogger _activityLogger;
    private readonly IUserFacingTextNormalizer _textNormalizer;

    public CreateItemCommandHandler(
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

    public async Task<Result<Guid>> Handle(CreateItemCommand request, CancellationToken cancellationToken)
    {
        var list = await _listRepository.GetByIdAsync(request.ListId, cancellationToken);
        if (list is null)
            return Result<Guid>.Failure(Error.NotFound("List not found."));

        if (list.Type != ListType.Shopping)
            return Result<Guid>.Failure(Error.Validation("Items are only supported for shopping lists."));

        var household = await _householdRepository.GetByIdWithMembersAsync(list.HouseholdId, cancellationToken);
        if (household is null || !household.IsMember(_currentUser.UserId))
            return Result<Guid>.Failure(Error.Forbidden("You are not a member of this household."));

        var title = _textNormalizer.NormalizeTitle(request.Title);
        var normalizedTitle = _textNormalizer.NormalizeForComparison(title);
        var existing = await FindExistingByNormalizedTitleAsync(request.ListId, normalizedTitle, cancellationToken);
        if (existing is not null)
        {
            if (!existing.IsCompleted)
                return Result<Guid>.Success(existing.Id);

            var tracked = await _itemRepository.GetByIdAsync(existing.Id, cancellationToken);
            if (tracked is null)
                return Result<Guid>.Failure(Error.NotFound("Item not found."));

            if (!tracked.IsCompleted)
                return Result<Guid>.Success(tracked.Id);

            tracked.Uncomplete(_currentUser.UserId);
            await _itemRepository.SaveChangesAsync(cancellationToken);

            var returningUser = await _userRepository.GetByIdAsync(_currentUser.UserId, cancellationToken);
            var returningUserName = returningUser?.DisplayName ?? "Unknown";
            await _notifications.NotifyItemUncompleted(list.HouseholdId, ToDto(tracked, returningUserName), cancellationToken);
            await _activityLogger.LogAsync(list.HouseholdId, ActivityEntityType.Item, tracked.Id, ActivityActionType.Uncompleted, _currentUser.UserId, tracked.Title, cancellationToken);

            return Result<Guid>.Success(tracked.Id);
        }

        var item = new ListItem(title, normalizedTitle, request.ListId, _currentUser.UserId, request.Quantity, request.Unit, request.Note);

        if (request.RecurrenceFrequency.HasValue && request.RecurrenceInterval.HasValue)
            item.SetRecurrence(request.RecurrenceFrequency.Value, request.RecurrenceInterval.Value);

        await _itemRepository.AddAsync(item, cancellationToken);
        await _itemRepository.SaveChangesAsync(cancellationToken);

        var user = await _userRepository.GetByIdAsync(_currentUser.UserId, cancellationToken);
        var dto = new ListItemDto(item.Id, item.Title, item.Quantity, item.Unit, item.Note,
            item.ListId, item.CreatedBy, user?.DisplayName ?? "Unknown", item.CreatedAt,
            item.IsCompleted, item.CompletedBy, null, item.CompletedAt,
            item.ReturnedToPendingBy, null, item.ReturnedToPendingAt,
            item.RecurrenceFrequency?.ToString(), item.RecurrenceInterval, item.NextDueDate);
        await _notifications.NotifyItemCreated(list.HouseholdId, dto, cancellationToken);
        await _activityLogger.LogAsync(list.HouseholdId, ActivityEntityType.Item, item.Id, ActivityActionType.Created, _currentUser.UserId, item.Title, cancellationToken);

        return Result<Guid>.Success(item.Id);
    }

    private async Task<ListItem?> FindExistingByNormalizedTitleAsync(Guid listId, string normalizedTitle, CancellationToken cancellationToken)
    {
        var existingItems = await _itemRepository.GetByListIdAsync(listId, "All", null, null, null, cancellationToken)
            ?? Array.Empty<ListItem>();

        return existingItems
            .Where(item => string.Equals(GetNormalizedTitle(item), normalizedTitle, StringComparison.Ordinal))
            .OrderBy(item => item.IsCompleted)
            .FirstOrDefault();
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
