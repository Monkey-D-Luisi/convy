using Convy.Application.Common.Interfaces;
using Convy.Application.Common.Models;
using Convy.Domain.Repositories;
using MediatR;

namespace Convy.Application.Features.Lists.Commands;

public class ArchiveListCommandHandler : IRequestHandler<ArchiveListCommand, Result>
{
    private readonly IHouseholdListRepository _listRepository;
    private readonly IHouseholdRepository _householdRepository;
    private readonly ICurrentUserService _currentUser;

    public ArchiveListCommandHandler(
        IHouseholdListRepository listRepository,
        IHouseholdRepository householdRepository,
        ICurrentUserService currentUser)
    {
        _listRepository = listRepository;
        _householdRepository = householdRepository;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(ArchiveListCommand request, CancellationToken cancellationToken)
    {
        var list = await _listRepository.GetByIdAsync(request.ListId, cancellationToken);

        if (list is null)
            return Result.Failure(Error.NotFound("List not found."));

        var household = await _householdRepository.GetByIdWithMembersAsync(list.HouseholdId, cancellationToken);

        if (household is null)
            return Result.Failure(Error.NotFound("Household not found."));

        if (!household.IsMember(_currentUser.UserId))
            return Result.Failure(Error.Forbidden("You are not a member of this household."));

        list.Archive();

        await _listRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
