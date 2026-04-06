using FluentValidation;

namespace Convy.Application.Features.Invites.Queries;

public class GetHouseholdInvitesQueryValidator : AbstractValidator<GetHouseholdInvitesQuery>
{
    public GetHouseholdInvitesQueryValidator()
    {
        RuleFor(x => x.HouseholdId)
            .NotEmpty().WithMessage("Household ID is required.");
    }
}
