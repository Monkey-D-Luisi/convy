using FluentValidation;

namespace Convy.Application.Features.Households.Commands;

public class CreateHouseholdCommandValidator : AbstractValidator<CreateHouseholdCommand>
{
    public CreateHouseholdCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Household name is required.")
            .MaximumLength(100).WithMessage("Household name must not exceed 100 characters.");
    }
}
