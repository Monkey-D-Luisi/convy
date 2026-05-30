using Convy.Application.Features.Tasks.Commands;
using Convy.Domain.ValueObjects;
using FluentValidation.TestHelper;

namespace Convy.Application.Tests.Features.Tasks;

public class TaskCommandValidatorTests
{
    [Fact]
    public void Update_WithEmptyListId_FailsValidation()
    {
        var result = new UpdateTaskCommandValidator().TestValidate(new UpdateTaskCommand(Guid.Empty, Guid.NewGuid(), "Clean kitchen", null));

        result.ShouldHaveValidationErrorFor(x => x.ListId);
    }

    [Fact]
    public void Update_WithEmptyTaskId_FailsValidation()
    {
        var result = new UpdateTaskCommandValidator().TestValidate(new UpdateTaskCommand(Guid.NewGuid(), Guid.Empty, "Clean kitchen", null));

        result.ShouldHaveValidationErrorFor(x => x.TaskId);
    }

    [Fact]
    public void Update_WithEmptyTitle_FailsValidation()
    {
        var result = new UpdateTaskCommandValidator().TestValidate(new UpdateTaskCommand(Guid.NewGuid(), Guid.NewGuid(), "", null));

        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Update_WithInvalidPriority_FailsValidation()
    {
        var result = new UpdateTaskCommandValidator().TestValidate(
            new UpdateTaskCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Clean kitchen",
                null,
                null,
                null,
                null,
                (TaskPriority)99));

        result.ShouldHaveValidationErrorFor(x => x.Priority);
    }

    [Fact]
    public void Create_WithReminderInPast_FailsValidation()
    {
        var result = new CreateTaskCommandValidator().TestValidate(
            new CreateTaskCommand(
                Guid.NewGuid(),
                "Clean kitchen",
                null,
                ReminderAtUtc: DateTime.UtcNow.AddMinutes(-5)));

        result.ShouldHaveValidationErrorFor(x => x.ReminderAtUtc);
    }

    [Fact]
    public void Create_WithLocalReminder_FailsValidation()
    {
        var result = new CreateTaskCommandValidator().TestValidate(
            new CreateTaskCommand(
                Guid.NewGuid(),
                "Clean kitchen",
                null,
                ReminderAtUtc: DateTime.SpecifyKind(DateTime.UtcNow.AddHours(1), DateTimeKind.Local)));

        result.ShouldHaveValidationErrorFor(x => x.ReminderAtUtc);
    }

    [Fact]
    public void Create_WithUnspecifiedReminder_FailsValidation()
    {
        var result = new CreateTaskCommandValidator().TestValidate(
            new CreateTaskCommand(
                Guid.NewGuid(),
                "Clean kitchen",
                null,
                ReminderAtUtc: DateTime.SpecifyKind(DateTime.UtcNow.AddHours(1), DateTimeKind.Unspecified)));

        result.ShouldHaveValidationErrorFor(x => x.ReminderAtUtc);
    }

    [Fact]
    public void Create_WithReminderWithoutDueDate_PassesValidation()
    {
        var result = new CreateTaskCommandValidator().TestValidate(
            new CreateTaskCommand(
                Guid.NewGuid(),
                "Clean kitchen",
                null,
                DueDate: null,
                ReminderAtUtc: DateTime.UtcNow.AddHours(1)));

        result.ShouldNotHaveValidationErrorFor(x => x.DueDate);
        result.ShouldNotHaveValidationErrorFor(x => x.ReminderAtUtc);
    }

    [Fact]
    public void Create_WithPastDueDate_PassesValidationAsOverdueTask()
    {
        var result = new CreateTaskCommandValidator().TestValidate(
            new CreateTaskCommand(
                Guid.NewGuid(),
                "Clean kitchen",
                null,
                DueDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1))));

        result.ShouldNotHaveValidationErrorFor(x => x.DueDate);
    }

    [Fact]
    public void SmartBatch_WithReminderInPast_FailsValidation()
    {
        var result = new SmartBatchCreateTasksCommandValidator().TestValidate(
            new SmartBatchCreateTasksCommand(
                Guid.NewGuid(),
                [
                    new SmartTaskInput(
                        "Clean kitchen",
                        null,
                        ReminderAtUtc: DateTime.UtcNow.AddMinutes(-5))
                ]));

        result.ShouldHaveValidationErrorFor("Tasks[0].ReminderAtUtc");
    }

    [Fact]
    public void Delete_WithEmptyListId_FailsValidation()
    {
        var result = new DeleteTaskCommandValidator().TestValidate(new DeleteTaskCommand(Guid.Empty, Guid.NewGuid()));

        result.ShouldHaveValidationErrorFor(x => x.ListId);
    }

    [Fact]
    public void Delete_WithEmptyTaskId_FailsValidation()
    {
        var result = new DeleteTaskCommandValidator().TestValidate(new DeleteTaskCommand(Guid.NewGuid(), Guid.Empty));

        result.ShouldHaveValidationErrorFor(x => x.TaskId);
    }

    [Fact]
    public void Complete_WithEmptyListId_FailsValidation()
    {
        var result = new CompleteTaskCommandValidator().TestValidate(new CompleteTaskCommand(Guid.Empty, Guid.NewGuid()));

        result.ShouldHaveValidationErrorFor(x => x.ListId);
    }

    [Fact]
    public void Complete_WithEmptyTaskId_FailsValidation()
    {
        var result = new CompleteTaskCommandValidator().TestValidate(new CompleteTaskCommand(Guid.NewGuid(), Guid.Empty));

        result.ShouldHaveValidationErrorFor(x => x.TaskId);
    }

    [Fact]
    public void Uncomplete_WithEmptyListId_FailsValidation()
    {
        var result = new UncompleteTaskCommandValidator().TestValidate(new UncompleteTaskCommand(Guid.Empty, Guid.NewGuid()));

        result.ShouldHaveValidationErrorFor(x => x.ListId);
    }

    [Fact]
    public void Uncomplete_WithEmptyTaskId_FailsValidation()
    {
        var result = new UncompleteTaskCommandValidator().TestValidate(new UncompleteTaskCommand(Guid.NewGuid(), Guid.Empty));

        result.ShouldHaveValidationErrorFor(x => x.TaskId);
    }
}
