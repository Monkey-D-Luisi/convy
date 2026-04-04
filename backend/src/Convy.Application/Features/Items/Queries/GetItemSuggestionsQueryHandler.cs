using Convy.Application.Common.Interfaces;
using Convy.Application.Common.Models;
using Convy.Application.Features.Items.DTOs;
using Convy.Domain.Repositories;
using MediatR;

namespace Convy.Application.Features.Items.Queries;

public class GetItemSuggestionsQueryHandler : IRequestHandler<GetItemSuggestionsQuery, Result<ItemSuggestionsDto>>
{
    private readonly IListItemRepository _itemRepository;
    private readonly IHouseholdRepository _householdRepository;
    private readonly ICurrentUserService _currentUser;

    public GetItemSuggestionsQueryHandler(
        IListItemRepository itemRepository,
        IHouseholdRepository householdRepository,
        ICurrentUserService currentUser)
    {
        _itemRepository = itemRepository;
        _householdRepository = householdRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<ItemSuggestionsDto>> Handle(GetItemSuggestionsQuery request, CancellationToken cancellationToken)
    {
        var household = await _householdRepository.GetByIdWithMembersAsync(request.HouseholdId, cancellationToken);
        if (household is null)
            return Result<ItemSuggestionsDto>.Failure(Error.NotFound("Household not found."));

        if (!household.IsMember(_currentUser.UserId))
            return Result<ItemSuggestionsDto>.Failure(Error.Forbidden("You are not a member of this household."));

        var titles = await _itemRepository.GetFrequentTitlesAsync(request.HouseholdId, request.Query, cancellationToken: cancellationToken);

        return Result<ItemSuggestionsDto>.Success(new ItemSuggestionsDto(titles));
    }
}
