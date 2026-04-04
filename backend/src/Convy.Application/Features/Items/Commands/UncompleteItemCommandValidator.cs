using FluentValidation;

namespace Convy.Application.Features.Items.Commands;

public class UncompleteItemCommandValidator : AbstractValidator<UncompleteItemCommand>
{
    public UncompleteItemCommandValidator()
    {
        RuleFor(x => x.ItemId)
            .NotEmpty().WithMessage("Item ID is required.");
    }
}
