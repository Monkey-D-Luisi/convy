using Convy.Application.Common.Interfaces;
using Convy.Application.Common.Models;
using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using MediatR;

namespace Convy.Application.Features.Households.Commands;

public class CreateHouseholdCommandHandler : IRequestHandler<CreateHouseholdCommand, Result<Guid>>
{
    private readonly IHouseholdRepository _householdRepository;
    private readonly ICurrentUserService _currentUser;

    public CreateHouseholdCommandHandler(
        IHouseholdRepository householdRepository,
        ICurrentUserService currentUser)
    {
        _householdRepository = householdRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(CreateHouseholdCommand request, CancellationToken cancellationToken)
    {
        var household = new Household(request.Name, _currentUser.UserId);

        await _householdRepository.AddAsync(household, cancellationToken);
        await _householdRepository.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(household.Id);
    }
}
