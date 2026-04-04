using FluentValidation;

namespace Convy.Application.Features.Invites.Commands;

public class JoinHouseholdCommandValidator : AbstractValidator<JoinHouseholdCommand>
{
    public JoinHouseholdCommandValidator()
    {
        RuleFor(x => x.InviteCode)
            .NotEmpty().WithMessage("Invite code is required.")
            .MaximumLength(20).WithMessage("Invite code is too long.");
    }
}
