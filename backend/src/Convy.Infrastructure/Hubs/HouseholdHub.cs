using Convy.Application.Common.Interfaces;
using Convy.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Convy.Infrastructure.Hubs;

[Authorize]
public class HouseholdHub : Hub
{
    private readonly IHouseholdRepository _householdRepository;
    private readonly ICurrentUserService _currentUser;

    public HouseholdHub(IHouseholdRepository householdRepository, ICurrentUserService currentUser)
    {
        _householdRepository = householdRepository;
        _currentUser = currentUser;
    }

    public async Task JoinHousehold(Guid householdId)
    {
        var household = await _householdRepository.GetByIdWithMembersAsync(householdId);
        if (household is null || !household.IsMember(_currentUser.UserId))
            throw new HubException("Forbidden");

        await Groups.AddToGroupAsync(Context.ConnectionId, householdId.ToString());
    }

    public async Task LeaveHousehold(Guid householdId)
    {
        var household = await _householdRepository.GetByIdWithMembersAsync(householdId);
        if (household is null || !household.IsMember(_currentUser.UserId))
            throw new HubException("Forbidden");

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, householdId.ToString());
    }
}
