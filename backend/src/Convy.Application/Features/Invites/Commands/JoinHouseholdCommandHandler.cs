using Convy.Application.Common.Interfaces;
using Convy.Application.Common.Models;
using Convy.Domain.Repositories;
using MediatR;

namespace Convy.Application.Features.Invites.Commands;

public class JoinHouseholdCommandHandler : IRequestHandler<JoinHouseholdCommand, Result<Guid>>
{
    private readonly IInviteRepository _inviteRepository;
    private readonly IHouseholdRepository _householdRepository;
    private readonly ICurrentUserService _currentUser;

    public JoinHouseholdCommandHandler(
        IInviteRepository inviteRepository,
        IHouseholdRepository householdRepository,
        ICurrentUserService currentUser)
    {
        _inviteRepository = inviteRepository;
        _householdRepository = householdRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(JoinHouseholdCommand request, CancellationToken cancellationToken)
    {
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

        household.AddMember(_currentUser.UserId);
        invite.Use(_currentUser.UserId);

        await _householdRepository.SaveChangesAsync(cancellationToken);
        await _inviteRepository.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(household.Id);
    }
}
