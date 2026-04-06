using Convy.Application.Common.Interfaces;
using Convy.Application.Common.Models;
using Convy.Application.Features.Activity.DTOs;
using Convy.Domain.Repositories;
using MediatR;

namespace Convy.Application.Features.Activity.Queries;

public class GetHouseholdActivityQueryHandler : IRequestHandler<GetHouseholdActivityQuery, Result<IReadOnlyList<ActivityLogDto>>>
{
    private readonly IActivityLogRepository _activityRepository;
    private readonly IHouseholdRepository _householdRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUser;

    public GetHouseholdActivityQueryHandler(
        IActivityLogRepository activityRepository,
        IHouseholdRepository householdRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUser)
    {
        _activityRepository = activityRepository;
        _householdRepository = householdRepository;
        _userRepository = userRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<ActivityLogDto>>> Handle(GetHouseholdActivityQuery request, CancellationToken cancellationToken)
    {
        var household = await _householdRepository.GetByIdWithMembersAsync(request.HouseholdId, cancellationToken);

        if (household is null)
            return Result<IReadOnlyList<ActivityLogDto>>.Failure(Error.NotFound("Household not found."));

        if (!household.IsMember(_currentUser.UserId))
            return Result<IReadOnlyList<ActivityLogDto>>.Failure(Error.Forbidden("You are not a member of this household."));

        var logs = await _activityRepository.GetByHouseholdIdAsync(request.HouseholdId, request.Limit, request.Before, cancellationToken);

        var performerIds = logs.Select(l => l.PerformedBy).Distinct().ToList();
        var users = new Dictionary<Guid, string>();
        foreach (var userId in performerIds)
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            users[userId] = user?.DisplayName ?? "Unknown";
        }

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
