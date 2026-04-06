using FluentValidation;

namespace Convy.Application.Features.Households.Commands;

public class LeaveHouseholdCommandValidator : AbstractValidator<LeaveHouseholdCommand>
{
    public LeaveHouseholdCommandValidator()
    {
        RuleFor(x => x.HouseholdId)
            .NotEmpty().WithMessage("Household ID is required.");
    }
}
