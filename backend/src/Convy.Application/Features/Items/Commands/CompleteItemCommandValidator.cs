using FluentValidation;

namespace Convy.Application.Features.Items.Commands;

public class CompleteItemCommandValidator : AbstractValidator<CompleteItemCommand>
{
    public CompleteItemCommandValidator()
    {
        RuleFor(x => x.ItemId)
            .NotEmpty().WithMessage("Item ID is required.");
    }
}
