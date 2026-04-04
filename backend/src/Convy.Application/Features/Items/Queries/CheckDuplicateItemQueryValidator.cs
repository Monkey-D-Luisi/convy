using FluentValidation;

namespace Convy.Application.Features.Items.Queries;

public class CheckDuplicateItemQueryValidator : AbstractValidator<CheckDuplicateItemQuery>
{
    public CheckDuplicateItemQueryValidator()
    {
        RuleFor(x => x.ListId)
            .NotEmpty().WithMessage("List ID is required.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");
    }
}
