using FluentValidation;

namespace Convy.Application.Features.Items.Commands;

public class BatchCreateItemsCommandValidator : AbstractValidator<BatchCreateItemsCommand>
{
    public BatchCreateItemsCommandValidator()
    {
        RuleFor(x => x.ListId)
            .NotEmpty().WithMessage("List ID is required.");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("At least one item is required.")
            .Must(items => items.Count <= 20).WithMessage("Cannot create more than 20 items at once.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Title)
                .NotEmpty().WithMessage("Item title is required.")
                .MaximumLength(200).WithMessage("Item title must not exceed 200 characters.");

            item.RuleFor(i => i.Quantity)
                .GreaterThan(0).When(i => i.Quantity.HasValue)
                .WithMessage("Quantity must be greater than zero.");

            item.RuleFor(i => i.Unit)
                .MaximumLength(50).When(i => i.Unit is not null)
                .WithMessage("Unit must not exceed 50 characters.");

            item.RuleFor(i => i.Note)
                .MaximumLength(500).When(i => i.Note is not null)
                .WithMessage("Note must not exceed 500 characters.");
        });
    }
}
