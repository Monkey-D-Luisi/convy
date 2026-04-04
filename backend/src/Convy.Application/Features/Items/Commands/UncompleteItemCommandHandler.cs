using Convy.Application.Common.Interfaces;
using Convy.Application.Common.Models;
using Convy.Domain.Repositories;
using MediatR;

namespace Convy.Application.Features.Items.Commands;

public class UncompleteItemCommandHandler : IRequestHandler<UncompleteItemCommand, Result>
{
    private readonly IListItemRepository _itemRepository;
    private readonly IHouseholdListRepository _listRepository;
    private readonly IHouseholdRepository _householdRepository;
    private readonly ICurrentUserService _currentUser;

    public UncompleteItemCommandHandler(
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

    public async Task<Result> Handle(UncompleteItemCommand request, CancellationToken cancellationToken)
    {
        var item = await _itemRepository.GetByIdAsync(request.ItemId, cancellationToken);
        if (item is null)
            return Result.Failure(Error.NotFound("Item not found."));

        var list = await _listRepository.GetByIdAsync(item.ListId, cancellationToken);
        if (list is null)
            return Result.Failure(Error.NotFound("List not found."));

        var household = await _householdRepository.GetByIdWithMembersAsync(list.HouseholdId, cancellationToken);
        if (household is null || !household.IsMember(_currentUser.UserId))
            return Result.Failure(Error.Forbidden("You are not a member of this household."));

        item.Uncomplete();

        await _itemRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
