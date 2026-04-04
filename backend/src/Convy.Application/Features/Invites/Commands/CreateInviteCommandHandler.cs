using Convy.Application.Common.Interfaces;
using Convy.Application.Common.Models;
using Convy.Application.Features.Invites.DTOs;
using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using MediatR;

namespace Convy.Application.Features.Invites.Commands;

public class CreateInviteCommandHandler : IRequestHandler<CreateInviteCommand, Result<InviteDto>>
{
    private readonly IInviteRepository _inviteRepository;
    private readonly IHouseholdRepository _householdRepository;
    private readonly ICurrentUserService _currentUser;

    public CreateInviteCommandHandler(
        IInviteRepository inviteRepository,
        IHouseholdRepository householdRepository,
        ICurrentUserService currentUser)
    {
        _inviteRepository = inviteRepository;
        _householdRepository = householdRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<InviteDto>> Handle(CreateInviteCommand request, CancellationToken cancellationToken)
    {
        var household = await _householdRepository.GetByIdWithMembersAsync(request.HouseholdId, cancellationToken);

        if (household is null)
            return Result<InviteDto>.Failure(Error.NotFound("Household not found."));

        if (!household.IsMember(_currentUser.UserId))
            return Result<InviteDto>.Failure(Error.Forbidden("You are not a member of this household."));

        var invite = new Invite(household.Id, _currentUser.UserId, TimeSpan.FromDays(7));

        await _inviteRepository.AddAsync(invite, cancellationToken);
        await _inviteRepository.SaveChangesAsync(cancellationToken);

        return Result<InviteDto>.Success(new InviteDto(
            invite.Id,
            invite.HouseholdId,
            invite.Code,
            invite.ExpiresAt,
            invite.IsValid,
            invite.CreatedAt));
    }
}
