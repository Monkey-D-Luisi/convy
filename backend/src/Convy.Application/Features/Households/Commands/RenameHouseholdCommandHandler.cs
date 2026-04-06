using Convy.Application.Common.Interfaces;
using Convy.Application.Common.Models;
using Convy.Domain.Repositories;
using Convy.Domain.ValueObjects;
using MediatR;

namespace Convy.Application.Features.Households.Commands;

public class RenameHouseholdCommandHandler : IRequestHandler<RenameHouseholdCommand, Result>
{
    private readonly IHouseholdRepository _householdRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IHouseholdNotificationService _notifications;
    private readonly IActivityLogger _activityLogger;

    public RenameHouseholdCommandHandler(
        IHouseholdRepository householdRepository,
        ICurrentUserService currentUser,
        IHouseholdNotificationService notifications,
        IActivityLogger activityLogger)
    {
        _householdRepository = householdRepository;
        _currentUser = currentUser;
        _notifications = notifications;
        _activityLogger = activityLogger;
    }

    public async Task<Result> Handle(RenameHouseholdCommand request, CancellationToken cancellationToken)
    {
        var household = await _householdRepository.GetByIdWithMembersAsync(request.HouseholdId, cancellationToken);

        if (household is null)
            return Result.Failure(Error.NotFound("Household not found."));

        if (!household.IsMember(_currentUser.UserId))
            return Result.Failure(Error.Forbidden("You are not a member of this household."));

        household.Rename(request.NewName);

        await _householdRepository.SaveChangesAsync(cancellationToken);

        await _notifications.NotifyHouseholdRenamed(household.Id, request.NewName, cancellationToken);
        await _activityLogger.LogAsync(household.Id, ActivityEntityType.Household, household.Id, ActivityActionType.Renamed, _currentUser.UserId, request.NewName, cancellationToken);

        return Result.Success();
    }
}
