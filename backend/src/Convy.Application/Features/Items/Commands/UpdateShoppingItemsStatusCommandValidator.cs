using FluentValidation;

namespace Convy.Application.Features.Items.Commands;

public class UpdateShoppingItemsStatusCommandValidator : AbstractValidator<UpdateShoppingItemsStatusCommand>
{
    public UpdateShoppingItemsStatusCommandValidator()
    {
        RuleFor(x => x.ListId).NotEmpty().WithMessage("List ID is required.");
        RuleFor(x => x.ItemIds)
            .NotEmpty().WithMessage("At least one item ID is required.")
            .Must(ids => ids.Count <= 20).WithMessage("Cannot process more than 20 items at once.");
        RuleForEach(x => x.ItemIds).NotEmpty().WithMessage("Item ID is required.");
        RuleFor(x => x.Status).IsInEnum().WithMessage("Invalid item status.");
    }
}
