using Convy.Application.Common.Interfaces;
using Convy.Application.Common.Models;
using Convy.Application.Features.Items.DTOs;
using Convy.Domain.Repositories;
using MediatR;

namespace Convy.Application.Features.Items.Commands;

public class UpdateItemCommandHandler : IRequestHandler<UpdateItemCommand, Result>
{
    private readonly IListItemRepository _itemRepository;
    private readonly IHouseholdListRepository _listRepository;
    private readonly IHouseholdRepository _householdRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IHouseholdNotificationService _notifications;

    public UpdateItemCommandHandler(
        IListItemRepository itemRepository,
        IHouseholdListRepository listRepository,
        IHouseholdRepository householdRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUser,
        IHouseholdNotificationService notifications)
    {
        _itemRepository = itemRepository;
        _listRepository = listRepository;
        _householdRepository = householdRepository;
        _userRepository = userRepository;
        _currentUser = currentUser;
        _notifications = notifications;
    }

    public async Task<Result> Handle(UpdateItemCommand request, CancellationToken cancellationToken)
    {
        var item = await _itemRepository.GetByIdAsync(request.ItemId, cancellationToken);
        if (item is null)
            return Result.Failure(Error.NotFound("Item not found."));

        var list = await _listRepository.GetByIdAsync(item.ListId, cancellationToken);
        if (list is null)
            return Result.Failure(Error.NotFound("List not found."));

        var household = await _householdRepository.GetByIdWithMembersAsync(list.HouseholdId, cancellationToken);
        if (household is null || !household.IsMember(_currentUser.UserId))
            return Result.Failure(Error.Forbidden("You are not a member of this household."));

        item.Update(request.Title, request.Quantity, request.Unit, request.Note);

        await _itemRepository.SaveChangesAsync(cancellationToken);

        var userIds = new[] { item.CreatedBy }.Concat(
            item.CompletedBy.HasValue ? new[] { item.CompletedBy.Value } : Array.Empty<Guid>()).Distinct();
        var users = await _userRepository.GetByIdsAsync(userIds, cancellationToken);
        var userNames = users.ToDictionary(u => u.Id, u => u.DisplayName);
        var dto = new ListItemDto(item.Id, item.Title, item.Quantity, item.Unit, item.Note,
            item.ListId, item.CreatedBy, userNames.GetValueOrDefault(item.CreatedBy, "Unknown"), item.CreatedAt,
            item.IsCompleted, item.CompletedBy,
            item.CompletedBy.HasValue ? userNames.GetValueOrDefault(item.CompletedBy.Value, "Unknown") : null,
            item.CompletedAt);
        await _notifications.NotifyItemUpdated(list.HouseholdId, dto, cancellationToken);

        return Result.Success();
    }
}
