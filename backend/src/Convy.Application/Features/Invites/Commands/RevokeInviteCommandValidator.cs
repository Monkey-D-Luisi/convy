using FluentValidation;

namespace Convy.Application.Features.Invites.Commands;

public class RevokeInviteCommandValidator : AbstractValidator<RevokeInviteCommand>
{
    public RevokeInviteCommandValidator()
    {
        RuleFor(x => x.InviteId)
            .NotEmpty().WithMessage("Invite ID is required.");
    }
}
