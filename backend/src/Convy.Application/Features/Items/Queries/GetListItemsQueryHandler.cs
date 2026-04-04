using Convy.Application.Common.Interfaces;
using Convy.Application.Common.Models;
using Convy.Application.Features.Items.DTOs;
using Convy.Domain.Repositories;
using MediatR;

namespace Convy.Application.Features.Items.Queries;

public class GetListItemsQueryHandler : IRequestHandler<GetListItemsQuery, Result<IReadOnlyList<ListItemDto>>>
{
    private readonly IListItemRepository _itemRepository;
    private readonly IHouseholdListRepository _listRepository;
    private readonly IHouseholdRepository _householdRepository;
    private readonly ICurrentUserService _currentUser;

    public GetListItemsQueryHandler(
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

    public async Task<Result<IReadOnlyList<ListItemDto>>> Handle(GetListItemsQuery request, CancellationToken cancellationToken)
    {
        var list = await _listRepository.GetByIdAsync(request.ListId, cancellationToken);
        if (list is null)
            return Result<IReadOnlyList<ListItemDto>>.Failure(Error.NotFound("List not found."));

        var household = await _householdRepository.GetByIdWithMembersAsync(list.HouseholdId, cancellationToken);
        if (household is null || !household.IsMember(_currentUser.UserId))
            return Result<IReadOnlyList<ListItemDto>>.Failure(Error.Forbidden("You are not a member of this household."));

        var items = await _itemRepository.GetByListIdAsync(request.ListId, request.IncludeCompleted, cancellationToken);

        var dtos = items.Select(i => new ListItemDto(
            i.Id,
            i.Title,
            i.Quantity,
            i.Unit,
            i.Note,
            i.ListId,
            i.CreatedBy,
            i.CreatedAt,
            i.IsCompleted,
            i.CompletedBy,
            i.CompletedAt)).ToList();

        return Result<IReadOnlyList<ListItemDto>>.Success(dtos);
    }
}
