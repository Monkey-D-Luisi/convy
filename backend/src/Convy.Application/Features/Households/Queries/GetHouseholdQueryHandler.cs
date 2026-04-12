using Convy.Application.Common.Interfaces;
using Convy.Application.Common.Models;
using Convy.Application.Features.Households.DTOs;
using Convy.Domain.Repositories;
using MediatR;

namespace Convy.Application.Features.Households.Queries;

public class GetHouseholdQueryHandler : IRequestHandler<GetHouseholdQuery, Result<HouseholdDetailDto>>
{
    private readonly IHouseholdRepository _householdRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUser;

    public GetHouseholdQueryHandler(
        IHouseholdRepository householdRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUser)
    {
        _householdRepository = householdRepository;
        _userRepository = userRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<HouseholdDetailDto>> Handle(GetHouseholdQuery request, CancellationToken cancellationToken)
    {
        var household = await _householdRepository.GetByIdWithMembersAsync(request.HouseholdId, cancellationToken);

        if (household is null)
            return Result<HouseholdDetailDto>.Failure(Error.NotFound("Household not found."));

        if (!household.IsMember(_currentUser.UserId))
            return Result<HouseholdDetailDto>.Failure(Error.Forbidden("You are not a member of this household."));

        var memberUserIds = household.Memberships.Select(m => m.UserId).ToList();
        var users = (await _userRepository.GetByIdsAsync(memberUserIds, cancellationToken))
            .ToDictionary(u => u.Id);

        var memberDtos = household.Memberships
            .Where(m => users.ContainsKey(m.UserId))
            .Select(m => new HouseholdMemberDto(
                users[m.UserId].Id,
                users[m.UserId].DisplayName,
                users[m.UserId].Email,
                m.Role,
                m.JoinedAt))
            .ToList();

        var dto = new HouseholdDetailDto(
            household.Id,
            household.Name,
            household.CreatedBy,
            household.CreatedAt,
            memberDtos);

        return Result<HouseholdDetailDto>.Success(dto);
    }
}
