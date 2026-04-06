using Convy.Application.Common.Interfaces;
using Convy.Application.Common.Models;
using Convy.Domain.Repositories;
using Convy.Domain.ValueObjects;
using MediatR;

namespace Convy.Application.Features.Households.Commands;

public class LeaveHouseholdCommandHandler : IRequestHandler<LeaveHouseholdCommand, Result>
{
    private readonly IHouseholdRepository _householdRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IHouseholdNotificationService _notifications;
    private readonly IActivityLogger _activityLogger;

    public LeaveHouseholdCommandHandler(
        IHouseholdRepository householdRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUser,
        IHouseholdNotificationService notifications,
        IActivityLogger activityLogger)
    {
        _householdRepository = householdRepository;
        _userRepository = userRepository;
        _currentUser = currentUser;
        _notifications = notifications;
        _activityLogger = activityLogger;
    }

    public async Task<Result> Handle(LeaveHouseholdCommand request, CancellationToken cancellationToken)
    {
        var household = await _householdRepository.GetByIdWithMembersAsync(request.HouseholdId, cancellationToken);

        if (household is null)
            return Result.Failure(Error.NotFound("Household not found."));

        if (!household.IsMember(_currentUser.UserId))
            return Result.Failure(Error.Forbidden("You are not a member of this household."));

        var user = await _userRepository.GetByIdAsync(_currentUser.UserId, cancellationToken);
        var displayName = user?.DisplayName ?? "Unknown";

        household.RemoveMember(_currentUser.UserId);

        await _householdRepository.SaveChangesAsync(cancellationToken);

        await _notifications.NotifyMemberLeft(request.HouseholdId, _currentUser.UserId, displayName, cancellationToken);
        await _activityLogger.LogAsync(request.HouseholdId, ActivityEntityType.Household, request.HouseholdId, ActivityActionType.MemberLeft, _currentUser.UserId, displayName, cancellationToken);

        return Result.Success();
    }
}
