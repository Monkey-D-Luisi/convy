using Convy.Application.Common.Interfaces;
using Convy.Application.Common.Models;
using Convy.Application.Features.Households.DTOs;
using Convy.Domain.Repositories;
using MediatR;

namespace Convy.Application.Features.Households.Queries;

public class GetMyHouseholdsQueryHandler : IRequestHandler<GetMyHouseholdsQuery, Result<IReadOnlyList<HouseholdDto>>>
{
    private readonly IHouseholdRepository _householdRepository;
    private readonly ICurrentUserService _currentUser;

    public GetMyHouseholdsQueryHandler(
        IHouseholdRepository householdRepository,
        ICurrentUserService currentUser)
    {
        _householdRepository = householdRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<HouseholdDto>>> Handle(GetMyHouseholdsQuery request, CancellationToken cancellationToken)
    {
        var households = await _householdRepository.GetByUserIdAsync(_currentUser.UserId, cancellationToken);

        var dtos = households.Select(h => new HouseholdDto(
            h.Id,
            h.Name,
            h.CreatedBy,
            h.CreatedAt)).ToList();

        return Result<IReadOnlyList<HouseholdDto>>.Success(dtos);
    }
}
