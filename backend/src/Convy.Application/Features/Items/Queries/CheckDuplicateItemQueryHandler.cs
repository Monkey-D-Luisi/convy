using Convy.Application.Common.Interfaces;
using Convy.Application.Common.Models;
using Convy.Application.Features.Items.DTOs;
using Convy.Domain.Repositories;
using MediatR;

namespace Convy.Application.Features.Items.Queries;

public class CheckDuplicateItemQueryHandler : IRequestHandler<CheckDuplicateItemQuery, Result<DuplicateCheckDto>>
{
    private readonly IListItemRepository _itemRepository;
    private readonly IHouseholdListRepository _listRepository;
    private readonly IHouseholdRepository _householdRepository;
    private readonly ICurrentUserService _currentUser;

    public CheckDuplicateItemQueryHandler(
        IListItemRepository itemRepository,
        IHouseholdListRepository listRepository,
        IHouseholdRepository householdRepository,
        ICurrentUserService currentUser)
    {
        _itemRepository = itemRepository;
        _listRepository = listRepository;
        _householdRepository = householdRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<DuplicateCheckDto>> Handle(CheckDuplicateItemQuery request, CancellationToken cancellationToken)
    {
        var list = await _listRepository.GetByIdAsync(request.ListId, cancellationToken);
        if (list is null)
            return Result<DuplicateCheckDto>.Failure(Error.NotFound("List not found."));

        var household = await _householdRepository.GetByIdWithMembersAsync(list.HouseholdId, cancellationToken);
        if (household is null || !household.IsMember(_currentUser.UserId))
            return Result<DuplicateCheckDto>.Failure(Error.Forbidden("You are not a member of this household."));

        var matches = await _itemRepository.SearchByTitleInListAsync(request.ListId, request.Title, cancellationToken);

        var duplicates = matches
            .Select(i => new DuplicateItemDto(i.Id, i.Title, i.Quantity, i.Unit))
            .ToList();

        return Result<DuplicateCheckDto>.Success(
            new DuplicateCheckDto(duplicates.Count > 0, duplicates));
    }
}
