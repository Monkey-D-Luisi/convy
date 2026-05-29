using FluentValidation;

namespace Convy.Application.Features.Tasks.Commands;

public class SmartBatchCreateTasksCommandValidator : AbstractValidator<SmartBatchCreateTasksCommand>
{
    public SmartBatchCreateTasksCommandValidator()
    {
        RuleFor(x => x.ListId)
            .NotEmpty().WithMessage("List ID is required.");

        RuleFor(x => x.Tasks)
            .NotEmpty().WithMessage("At least one task is required.")
            .Must(tasks => tasks.Count <= 20).WithMessage("Cannot process more than 20 tasks at once.");

        RuleForEach(x => x.Tasks).ChildRules(task =>
        {
            task.RuleFor(i => i.Title)
                .NotNull().WithMessage("Task title is required.")
                .MaximumLength(200).WithMessage("Task title must not exceed 200 characters.");

            task.RuleFor(i => i.Note)
                .MaximumLength(500).When(i => i.Note is not null)
                .WithMessage("Note must not exceed 500 characters.");

            task.RuleFor(i => i.AssignedToUserId)
                .NotEqual(Guid.Empty).When(i => i.AssignedToUserId.HasValue)
                .WithMessage("Assigned user ID must not be empty.");

            task.RuleFor(i => i.Priority)
                .IsInEnum().WithMessage("Invalid task priority.");
        });
    }
}
