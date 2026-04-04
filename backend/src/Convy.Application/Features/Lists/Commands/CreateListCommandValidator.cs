using FluentValidation;

namespace Convy.Application.Features.Lists.Commands;

public class CreateListCommandValidator : AbstractValidator<CreateListCommand>
{
    public CreateListCommandValidator()
    {
        RuleFor(x => x.HouseholdId)
            .NotEmpty().WithMessage("Household ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("List name is required.")
            .MaximumLength(100).WithMessage("List name must not exceed 100 characters.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("List type must be a valid value.");
    }
}
