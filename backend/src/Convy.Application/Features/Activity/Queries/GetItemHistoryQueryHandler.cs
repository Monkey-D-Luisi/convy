using Convy.Application.Common.Interfaces;
using Convy.Application.Common.Models;
using Convy.Application.Features.Activity.DTOs;
using Convy.Domain.Repositories;
using MediatR;

namespace Convy.Application.Features.Activity.Queries;

public class GetItemHistoryQueryHandler : IRequestHandler<GetItemHistoryQuery, Result<IReadOnlyList<ActivityLogDto>>>
{
    private readonly IActivityLogRepository _activityRepository;
    private readonly IListItemRepository _itemRepository;
    private readonly IHouseholdListRepository _listRepository;
    private readonly IHouseholdRepository _householdRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUser;

    public GetItemHistoryQueryHandler(
        IActivityLogRepository activityRepository,
        IListItemRepository itemRepository,
        IHouseholdListRepository listRepository,
        IHouseholdRepository householdRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUser)
    {
        _activityRepository = activityRepository;
        _itemRepository = itemRepository;
        _listRepository = listRepository;
        _householdRepository = householdRepository;
        _userRepository = userRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<ActivityLogDto>>> Handle(GetItemHistoryQuery request, CancellationToken cancellationToken)
    {
        var item = await _itemRepository.GetByIdAsync(request.ItemId, cancellationToken);
        if (item is null)
            return Result<IReadOnlyList<ActivityLogDto>>.Failure(Error.NotFound("Item not found."));

        var list = await _listRepository.GetByIdAsync(item.ListId, cancellationToken);
        if (list is null)
            return Result<IReadOnlyList<ActivityLogDto>>.Failure(Error.NotFound("List not found."));

        var household = await _householdRepository.GetByIdWithMembersAsync(list.HouseholdId, cancellationToken);
        if (household is null || !household.IsMember(_currentUser.UserId))
            return Result<IReadOnlyList<ActivityLogDto>>.Failure(Error.Forbidden("You are not a member of this household."));

        var logs = await _activityRepository.GetByEntityIdAsync(request.ItemId, 50, cancellationToken);

        var performerIds = logs.Select(l => l.PerformedBy).Distinct().ToList();
        var users = (await _userRepository.GetByIdsAsync(performerIds, cancellationToken))
            .ToDictionary(u => u.Id, u => u.DisplayName);

        var dtos = logs.Select(log => new ActivityLogDto(
            log.Id,
            log.HouseholdId,
            log.EntityType,
            log.EntityId,
            log.ActionType,
            log.PerformedBy,
            users.GetValueOrDefault(log.PerformedBy, "Unknown"),
            log.CreatedAt,
            log.Metadata
        )).ToList().AsReadOnly();

        return Result<IReadOnlyList<ActivityLogDto>>.Success(dtos);
    }
}
