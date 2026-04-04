using FluentValidation;

namespace Convy.Application.Features.Lists.Commands;

public class RenameListCommandValidator : AbstractValidator<RenameListCommand>
{
    public RenameListCommandValidator()
    {
        RuleFor(x => x.ListId)
            .NotEmpty().WithMessage("List ID is required.");

        RuleFor(x => x.NewName)
            .NotEmpty().WithMessage("New name is required.")
            .MaximumLength(100).WithMessage("List name must not exceed 100 characters.");
    }
}
