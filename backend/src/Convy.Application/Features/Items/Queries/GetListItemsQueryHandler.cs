using Convy.Application.Common.Interfaces;
using Convy.Application.Common.Models;
using Convy.Application.Features.Items.DTOs;
using Convy.Domain.Repositories;
using Convy.Domain.ValueObjects;
using MediatR;

namespace Convy.Application.Features.Items.Queries;

public class GetListItemsQueryHandler : IRequestHandler<GetListItemsQuery, Result<IReadOnlyList<ListItemDto>>>
{
    private readonly IListItemRepository _itemRepository;
    private readonly IHouseholdListRepository _listRepository;
    private readonly IHouseholdRepository _householdRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUser;

    public GetListItemsQueryHandler(
        IListItemRepository itemRepository,
        IHouseholdListRepository listRepository,
        IHouseholdRepository householdRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUser)
    {
        _itemRepository = itemRepository;
        _listRepository = listRepository;
        _householdRepository = householdRepository;
        _userRepository = userRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<ListItemDto>>> Handle(GetListItemsQuery request, CancellationToken cancellationToken)
    {
        var list = await _listRepository.GetByIdAsync(request.ListId, cancellationToken);
        if (list is null)
            return Result<IReadOnlyList<ListItemDto>>.Failure(Error.NotFound("List not found."));

        if (list.Type != ListType.Shopping)
            return Result<IReadOnlyList<ListItemDto>>.Failure(Error.Validation("Items are only supported for shopping lists."));

        var household = await _householdRepository.GetByIdWithMembersAsync(list.HouseholdId, cancellationToken);
        if (household is null || !household.IsMember(_currentUser.UserId))
            return Result<IReadOnlyList<ListItemDto>>.Failure(Error.Forbidden("You are not a member of this household."));

        var items = await _itemRepository.GetByListIdAsync(
            request.ListId,
            request.Status,
            request.CreatedBy,
            request.FromDate,
            request.ToDate,
            cancellationToken);

        var userIds = items.Select(i => i.CreatedBy)
            .Concat(items.Where(i => i.CompletedBy.HasValue).Select(i => i.CompletedBy!.Value))
            .Distinct();
        var users = await _userRepository.GetByIdsAsync(userIds, cancellationToken);
        var userNames = users.ToDictionary(u => u.Id, u => u.DisplayName);

        var dtos = items.Select(i => new ListItemDto(
            i.Id,
            i.Title,
            i.Quantity,
            i.Unit,
            i.Note,
            i.ListId,
            i.CreatedBy,
            userNames.GetValueOrDefault(i.CreatedBy, "Unknown"),
            i.CreatedAt,
            i.IsCompleted,
            i.CompletedBy,
            i.CompletedBy.HasValue ? userNames.GetValueOrDefault(i.CompletedBy.Value, "Unknown") : null,
            i.CompletedAt,
            i.RecurrenceFrequency?.ToString(),
            i.RecurrenceInterval,
            i.NextDueDate)).ToList();

        return Result<IReadOnlyList<ListItemDto>>.Success(dtos);
    }
}
