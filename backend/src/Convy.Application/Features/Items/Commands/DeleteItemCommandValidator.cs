using FluentValidation;

namespace Convy.Application.Features.Items.Commands;

public class DeleteItemCommandValidator : AbstractValidator<DeleteItemCommand>
{
    public DeleteItemCommandValidator()
    {
        RuleFor(x => x.ListId)
            .NotEmpty().WithMessage("List ID is required.");

        RuleFor(x => x.ItemId)
            .NotEmpty().WithMessage("Item ID is required.");
    }
}
