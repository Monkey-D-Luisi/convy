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
            .MaximumLength(TaskInputLimits.TitleMaxLength).WithMessage("Task title must not exceed 80 characters.");

        RuleFor(x => x.Note)
            .MaximumLength(TaskInputLimits.NoteMaxLength).When(x => x.Note is not null)
            .WithMessage("Note must not exceed 500 characters.");

        RuleFor(x => x.AssignedToUserId)
            .NotEqual(Guid.Empty).When(x => x.AssignedToUserId.HasValue)
            .WithMessage("Assigned user ID must not be empty.");

        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("Invalid task priority.");

        RuleFor(x => x.ReminderAtUtc)
            .Must(BeUtc).When(x => x.ReminderAtUtc.HasValue)
            .WithMessage("Reminder timestamp must be UTC.");

        RuleFor(x => x.ReminderAtUtc)
            .Must(BeFutureReminder).When(x => x.ReminderAtUtc.HasValue)
            .WithMessage("Reminder timestamp must be in the future.");
    }

    private static bool BeUtc(DateTime? value) =>
        value is null || value.Value.Kind == DateTimeKind.Utc;

    private static bool BeFutureReminder(DateTime? value) =>
        value is null || value.Value > DateTime.UtcNow;
}
