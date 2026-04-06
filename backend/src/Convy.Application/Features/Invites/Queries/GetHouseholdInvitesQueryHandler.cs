using Convy.Application.Common.Interfaces;
using Convy.Application.Common.Models;
using Convy.Application.Features.Invites.DTOs;
using Convy.Domain.Repositories;
using MediatR;

namespace Convy.Application.Features.Invites.Queries;

public class GetHouseholdInvitesQueryHandler : IRequestHandler<GetHouseholdInvitesQuery, Result<List<InviteDto>>>
{
    private readonly IInviteRepository _inviteRepository;
    private readonly IHouseholdRepository _householdRepository;
    private readonly ICurrentUserService _currentUser;

    public GetHouseholdInvitesQueryHandler(
        IInviteRepository inviteRepository,
        IHouseholdRepository householdRepository,
        ICurrentUserService currentUser)
    {
        _inviteRepository = inviteRepository;
        _householdRepository = householdRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<List<InviteDto>>> Handle(GetHouseholdInvitesQuery request, CancellationToken cancellationToken)
    {
        var household = await _householdRepository.GetByIdWithMembersAsync(request.HouseholdId, cancellationToken);

        if (household is null)
            return Result<List<InviteDto>>.Failure(Error.NotFound("Household not found."));

        if (!household.IsMember(_currentUser.UserId))
            return Result<List<InviteDto>>.Failure(Error.Forbidden("You are not a member of this household."));

        var invites = await _inviteRepository.GetByHouseholdIdAsync(request.HouseholdId, cancellationToken);

        var dtos = invites
            .Where(i => i.IsValid)
            .Select(i => new InviteDto(i.Id, i.HouseholdId, i.Code, i.ExpiresAt, i.IsValid, i.CreatedAt))
            .ToList();

        return Result<List<InviteDto>>.Success(dtos);
    }
}
