using FluentValidation;

namespace Convy.Application.Features.Items.Commands;

public class CreateItemCommandValidator : AbstractValidator<CreateItemCommand>
{
    public CreateItemCommandValidator()
    {
        RuleFor(x => x.ListId)
            .NotEmpty().WithMessage("List ID is required.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Item title is required.")
            .MaximumLength(200).WithMessage("Item title must not exceed 200 characters.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).When(x => x.Quantity.HasValue)
            .WithMessage("Quantity must be greater than zero.");

        RuleFor(x => x.Unit)
            .MaximumLength(50).When(x => x.Unit is not null)
            .WithMessage("Unit must not exceed 50 characters.");

        RuleFor(x => x.Note)
            .MaximumLength(500).When(x => x.Note is not null)
            .WithMessage("Note must not exceed 500 characters.");

        RuleFor(x => x.RecurrenceInterval)
            .GreaterThan(0).When(x => x.RecurrenceInterval.HasValue)
            .WithMessage("Recurrence interval must be greater than zero.");

        RuleFor(x => x.RecurrenceFrequency)
            .IsInEnum().When(x => x.RecurrenceFrequency.HasValue)
            .WithMessage("Invalid recurrence frequency.");
    }
}
