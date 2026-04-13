using Convy.Application.Common.Interfaces;
using Convy.Application.Common.Models;
using Convy.Domain.Repositories;
using Convy.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Convy.Application.Features.Invites.Commands;

public class JoinHouseholdCommandHandler : IRequestHandler<JoinHouseholdCommand, Result<Guid>>
{
    private readonly IInviteRepository _inviteRepository;
    private readonly IHouseholdRepository _householdRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IHouseholdNotificationService _notifications;
    private readonly IActivityLogger _activityLogger;
    private readonly ILogger<JoinHouseholdCommandHandler> _logger;

    public JoinHouseholdCommandHandler(
        IInviteRepository inviteRepository,
        IHouseholdRepository householdRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUser,
        IHouseholdNotificationService notifications,
        IActivityLogger activityLogger,
        ILogger<JoinHouseholdCommandHandler> logger)
    {
        _inviteRepository = inviteRepository;
        _householdRepository = householdRepository;
        _userRepository = userRepository;
        _currentUser = currentUser;
        _notifications = notifications;
        _activityLogger = activityLogger;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(JoinHouseholdCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == Guid.Empty)
            return Result<Guid>.Failure(Error.Validation("User account not found. Please sign in again."));

        var currentUser = await _userRepository.GetByIdAsync(_currentUser.UserId, cancellationToken);
        if (currentUser is null)
            return Result<Guid>.Failure(Error.Validation("User account not registered. Please restart the app."));

        var invite = await _inviteRepository.GetByCodeAsync(request.InviteCode, cancellationToken);

        if (invite is null)
            return Result<Guid>.Failure(Error.NotFound("Invite not found."));

        if (!invite.IsValid)
            return Result<Guid>.Failure(Error.Validation("Invite is expired or already used."));

        var household = await _householdRepository.GetByIdWithMembersAsync(invite.HouseholdId, cancellationToken);

        if (household is null)
            return Result<Guid>.Failure(Error.NotFound("Household not found."));

        if (household.IsMember(_currentUser.UserId))
            return Result<Guid>.Failure(Error.Conflict("You are already a member of this household."));

        _logger.LogInformation(
            "Adding member {UserId} to household {HouseholdId} via invite {InviteId}",
            _currentUser.UserId, household.Id, invite.Id);

        household.AddMember(_currentUser.UserId);
        await _householdRepository.SaveChangesAsync(cancellationToken);

        invite.Use(_currentUser.UserId);
        await _inviteRepository.SaveChangesAsync(cancellationToken);

        var displayName = currentUser.DisplayName ?? _currentUser.UserId.ToString();
        await _notifications.NotifyMemberJoined(household.Id, _currentUser.UserId.ToString(), displayName, cancellationToken);
        await _activityLogger.LogAsync(household.Id, ActivityEntityType.Household, household.Id, ActivityActionType.MemberJoined, _currentUser.UserId, cancellationToken: cancellationToken);

        return Result<Guid>.Success(household.Id);
    }
}
