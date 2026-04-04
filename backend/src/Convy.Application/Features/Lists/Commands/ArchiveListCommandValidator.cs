using FluentValidation;

namespace Convy.Application.Features.Lists.Commands;

public class ArchiveListCommandValidator : AbstractValidator<ArchiveListCommand>
{
    public ArchiveListCommandValidator()
    {
        RuleFor(x => x.ListId)
            .NotEmpty().WithMessage("List ID is required.");
    }
}
