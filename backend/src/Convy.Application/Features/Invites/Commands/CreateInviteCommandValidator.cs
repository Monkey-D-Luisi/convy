using FluentValidation;

namespace Convy.Application.Features.Invites.Commands;

public class CreateInviteCommandValidator : AbstractValidator<CreateInviteCommand>
{
    public CreateInviteCommandValidator()
    {
        RuleFor(x => x.HouseholdId)
            .NotEmpty().WithMessage("Household ID is required.");
    }
}
