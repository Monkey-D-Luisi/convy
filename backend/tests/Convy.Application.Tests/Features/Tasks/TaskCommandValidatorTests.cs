using Convy.Application.Features.Tasks.Commands;
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
