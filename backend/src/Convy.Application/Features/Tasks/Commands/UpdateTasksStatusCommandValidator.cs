using FluentValidation;

namespace Convy.Application.Features.Tasks.Commands;

public class UpdateTasksStatusCommandValidator : AbstractValidator<UpdateTasksStatusCommand>
{
    public UpdateTasksStatusCommandValidator()
    {
        RuleFor(x => x.ListId).NotEmpty().WithMessage("List ID is required.");
        RuleFor(x => x.TaskIds)
            .NotEmpty().WithMessage("At least one task ID is required.")
            .Must(ids => ids.Count <= 20).WithMessage("Cannot process more than 20 tasks at once.");
        RuleForEach(x => x.TaskIds).NotEmpty().WithMessage("Task ID is required.");
        RuleFor(x => x.Status).IsInEnum().WithMessage("Invalid task status.");
    }
}
