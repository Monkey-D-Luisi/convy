using FluentValidation;

namespace Convy.Application.Features.Tasks.Commands;

public class UncompleteTaskCommandValidator : AbstractValidator<UncompleteTaskCommand>
{
    public UncompleteTaskCommandValidator()
    {
        RuleFor(x => x.ListId)
            .NotEmpty().WithMessage("List ID is required.");

        RuleFor(x => x.TaskId)
            .NotEmpty().WithMessage("Task ID is required.");
    }
}
