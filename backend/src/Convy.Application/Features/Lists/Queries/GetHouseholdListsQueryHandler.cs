using Convy.Application.Common.Interfaces;
using Convy.Application.Common.Models;
using Convy.Application.Features.Lists.DTOs;
using Convy.Domain.Repositories;
using MediatR;

namespace Convy.Application.Features.Lists.Queries;

public class GetHouseholdListsQueryHandler : IRequestHandler<GetHouseholdListsQuery, Result<IReadOnlyList<HouseholdListDto>>>
{
    private readonly IHouseholdListRepository _listRepository;
    private readonly IHouseholdRepository _householdRepository;
    private readonly ICurrentUserService _currentUser;

    public GetHouseholdListsQueryHandler(
        IHouseholdListRepository listRepository,
        IHouseholdRepository householdRepository,
        ICurrentUserService currentUser)
    {
        _listRepository = listRepository;
        _householdRepository = householdRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<HouseholdListDto>>> Handle(GetHouseholdListsQuery request, CancellationToken cancellationToken)
    {
        var household = await _householdRepository.GetByIdWithMembersAsync(request.HouseholdId, cancellationToken);

        if (household is null)
            return Result<IReadOnlyList<HouseholdListDto>>.Failure(Error.NotFound("Household not found."));

        if (!household.IsMember(_currentUser.UserId))
            return Result<IReadOnlyList<HouseholdListDto>>.Failure(Error.Forbidden("You are not a member of this household."));

        var lists = await _listRepository.GetByHouseholdIdAsync(request.HouseholdId, request.IncludeArchived, cancellationToken);

        var dtos = lists.Select(l => new HouseholdListDto(
            l.Id,
            l.Name,
            l.Type,
            l.HouseholdId,
            l.CreatedBy,
            l.CreatedAt,
            l.IsArchived,
            l.ArchivedAt)).ToList();

        return Result<IReadOnlyList<HouseholdListDto>>.Success(dtos);
    }
}
