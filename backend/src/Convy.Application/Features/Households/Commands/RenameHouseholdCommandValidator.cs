using FluentValidation;

namespace Convy.Application.Features.Households.Commands;

public class RenameHouseholdCommandValidator : AbstractValidator<RenameHouseholdCommand>
{
    public RenameHouseholdCommandValidator()
    {
        RuleFor(x => x.HouseholdId)
            .NotEmpty().WithMessage("Household ID is required.");

        RuleFor(x => x.NewName)
            .NotEmpty().WithMessage("New name is required.")
            .MaximumLength(100).WithMessage("Household name must not exceed 100 characters.");
    }
}
