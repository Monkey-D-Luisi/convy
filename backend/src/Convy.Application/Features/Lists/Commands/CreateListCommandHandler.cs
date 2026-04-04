using Convy.Application.Common.Interfaces;
using Convy.Application.Common.Models;
using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using MediatR;

namespace Convy.Application.Features.Lists.Commands;

public class CreateListCommandHandler : IRequestHandler<CreateListCommand, Result<Guid>>
{
    private readonly IHouseholdListRepository _listRepository;
    private readonly IHouseholdRepository _householdRepository;
    private readonly ICurrentUserService _currentUser;

    public CreateListCommandHandler(
        IHouseholdListRepository listRepository,
        IHouseholdRepository householdRepository,
        ICurrentUserService currentUser)
    {
        _listRepository = listRepository;
        _householdRepository = householdRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(CreateListCommand request, CancellationToken cancellationToken)
    {
        var household = await _householdRepository.GetByIdWithMembersAsync(request.HouseholdId, cancellationToken);

        if (household is null)
            return Result<Guid>.Failure(Error.NotFound("Household not found."));

        if (!household.IsMember(_currentUser.UserId))
            return Result<Guid>.Failure(Error.Forbidden("You are not a member of this household."));

        var list = new HouseholdList(request.Name, request.Type, request.HouseholdId, _currentUser.UserId);

        await _listRepository.AddAsync(list, cancellationToken);
        await _listRepository.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(list.Id);
    }
}
