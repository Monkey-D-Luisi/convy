using Convy.Application.Common.Interfaces;
using Convy.Application.Common.Models;
using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using Convy.Domain.ValueObjects;
using MediatR;

namespace Convy.Application.Features.Lists.Commands;

public class CreateListCommandHandler : IRequestHandler<CreateListCommand, Result<Guid>>
{
    private readonly IHouseholdListRepository _listRepository;
    private readonly IHouseholdRepository _householdRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IHouseholdNotificationService _notifications;
    private readonly IActivityLogger _activityLogger;

    public CreateListCommandHandler(
        IHouseholdListRepository listRepository,
        IHouseholdRepository householdRepository,
        ICurrentUserService currentUser,
        IHouseholdNotificationService notifications,
        IActivityLogger activityLogger)
    {
        _listRepository = listRepository;
        _householdRepository = householdRepository;
        _currentUser = currentUser;
        _notifications = notifications;
        _activityLogger = activityLogger;
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

        await _notifications.NotifyListCreated(request.HouseholdId, list.Id, request.Name, cancellationToken);
        await _activityLogger.LogAsync(request.HouseholdId, ActivityEntityType.List, list.Id, ActivityActionType.Created, _currentUser.UserId, request.Name, cancellationToken);

        return Result<Guid>.Success(list.Id);
    }
}
