using Convy.Application.Common.Interfaces;
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

    public CreateItemCommandHandler(
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

    public async Task<Result<Guid>> Handle(CreateItemCommand request, CancellationToken cancellationToken)
    {
        var list = await _listRepository.GetByIdAsync(request.ListId, cancellationToken);
        if (list is null)
            return Result<Guid>.Failure(Error.NotFound("List not found."));

        var household = await _householdRepository.GetByIdWithMembersAsync(list.HouseholdId, cancellationToken);
        if (household is null || !household.IsMember(_currentUser.UserId))
            return Result<Guid>.Failure(Error.Forbidden("You are not a member of this household."));

        var item = new ListItem(request.Title, request.ListId, _currentUser.UserId, request.Quantity, request.Unit, request.Note);

        if (request.RecurrenceFrequency.HasValue && request.RecurrenceInterval.HasValue)
            item.SetRecurrence(request.RecurrenceFrequency.Value, request.RecurrenceInterval.Value);

        await _itemRepository.AddAsync(item, cancellationToken);
        await _itemRepository.SaveChangesAsync(cancellationToken);

        var user = await _userRepository.GetByIdAsync(_currentUser.UserId, cancellationToken);
        var dto = new ListItemDto(item.Id, item.Title, item.Quantity, item.Unit, item.Note,
            item.ListId, item.CreatedBy, user?.DisplayName ?? "Unknown", item.CreatedAt,
            item.IsCompleted, item.CompletedBy, null, item.CompletedAt,
            item.RecurrenceFrequency?.ToString(), item.RecurrenceInterval, item.NextDueDate);
        await _notifications.NotifyItemCreated(list.HouseholdId, dto, cancellationToken);
        await _activityLogger.LogAsync(list.HouseholdId, ActivityEntityType.Item, item.Id, ActivityActionType.Created, _currentUser.UserId, item.Title, cancellationToken);

        return Result<Guid>.Success(item.Id);
    }
}
