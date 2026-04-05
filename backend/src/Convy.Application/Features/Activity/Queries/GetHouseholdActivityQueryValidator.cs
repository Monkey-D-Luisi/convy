using FluentValidation;

namespace Convy.Application.Features.Activity.Queries;

public class GetHouseholdActivityQueryValidator : AbstractValidator<GetHouseholdActivityQuery>
{
    public GetHouseholdActivityQueryValidator()
    {
        RuleFor(x => x.HouseholdId)
            .NotEmpty().WithMessage("Household ID is required.");

        RuleFor(x => x.Limit)
            .GreaterThan(0).WithMessage("Limit must be greater than zero.")
            .LessThanOrEqualTo(200).WithMessage("Limit must not exceed 200.");
    }
}
