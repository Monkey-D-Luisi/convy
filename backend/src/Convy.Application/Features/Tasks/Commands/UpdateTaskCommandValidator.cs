using FluentValidation;

namespace Convy.Application.Features.Tasks.Commands;

public class UpdateTaskCommandValidator : AbstractValidator<UpdateTaskCommand>
{
    public UpdateTaskCommandValidator()
    {
        RuleFor(x => x.ListId)
            .NotEmpty().WithMessage("List ID is required.");

        RuleFor(x => x.TaskId)
            .NotEmpty().WithMessage("Task ID is required.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Task title is required.")
            .MaximumLength(200).WithMessage("Task title must not exceed 200 characters.");

        RuleFor(x => x.Note)
            .MaximumLength(500).When(x => x.Note is not null)
            .WithMessage("Note must not exceed 500 characters.");
    }
}
