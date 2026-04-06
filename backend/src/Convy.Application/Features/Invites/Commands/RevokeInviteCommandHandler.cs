using Convy.Application.Common.Interfaces;
using Convy.Application.Common.Models;
using Convy.Domain.Repositories;
using Convy.Domain.ValueObjects;
using MediatR;

namespace Convy.Application.Features.Invites.Commands;

public class RevokeInviteCommandHandler : IRequestHandler<RevokeInviteCommand, Result>
{
    private readonly IInviteRepository _inviteRepository;
    private readonly IHouseholdRepository _householdRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IActivityLogger _activityLogger;

    public RevokeInviteCommandHandler(
        IInviteRepository inviteRepository,
        IHouseholdRepository householdRepository,
        ICurrentUserService currentUser,
        IActivityLogger activityLogger)
    {
        _inviteRepository = inviteRepository;
        _householdRepository = householdRepository;
        _currentUser = currentUser;
        _activityLogger = activityLogger;
    }

    public async Task<Result> Handle(RevokeInviteCommand request, CancellationToken cancellationToken)
    {
        var invite = await _inviteRepository.GetByIdAsync(request.InviteId, cancellationToken);

        if (invite is null)
            return Result.Failure(Error.NotFound("Invite not found."));

        var household = await _householdRepository.GetByIdWithMembersAsync(invite.HouseholdId, cancellationToken);

        if (household is null)
            return Result.Failure(Error.NotFound("Household not found."));

        if (!household.IsMember(_currentUser.UserId))
            return Result.Failure(Error.Forbidden("You are not a member of this household."));

        invite.Revoke();

        await _inviteRepository.SaveChangesAsync(cancellationToken);

        await _activityLogger.LogAsync(invite.HouseholdId, ActivityEntityType.Invite, invite.Id, ActivityActionType.Revoked, _currentUser.UserId, cancellationToken: cancellationToken);

        return Result.Success();
    }
}
