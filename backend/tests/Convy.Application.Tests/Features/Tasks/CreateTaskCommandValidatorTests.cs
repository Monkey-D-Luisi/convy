using Convy.Application.Features.Tasks.Commands;
using FluentValidation.TestHelper;

namespace Convy.Application.Tests.Features.Tasks;

public class CreateTaskCommandValidatorTests
{
    private readonly CreateTaskCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidData_PassesValidation()
    {
        var command = new CreateTaskCommand(Guid.NewGuid(), "Clean kitchen", "Before dinner");

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyListId_FailsValidation()
    {
        var command = new CreateTaskCommand(Guid.Empty, "Clean kitchen", null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.ListId);
    }

    [Fact]
    public void Validate_WithEmptyTitle_FailsValidation()
    {
        var command = new CreateTaskCommand(Guid.NewGuid(), "", null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Validate_WithLongNote_FailsValidation()
    {
        var command = new CreateTaskCommand(Guid.NewGuid(), "Clean kitchen", new string('a', 501));

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Note);
    }
}
