using Convy.Application.Features.Users.Commands;
using FluentValidation.TestHelper;

namespace Convy.Application.Tests.Features.Users;

public class RegisterUserCommandValidatorTests
{
    private readonly RegisterUserCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidData_PassesValidation()
    {
        var command = new RegisterUserCommand("firebase-uid-123", "John Doe", "john@example.com");
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyDisplayName_FailsValidation()
    {
        var command = new RegisterUserCommand("firebase-uid-123", "", "john@example.com");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.DisplayName);
    }

    [Fact]
    public void Validate_WithLongDisplayName_FailsValidation()
    {
        var command = new RegisterUserCommand("firebase-uid-123", new string('a', 101), "john@example.com");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.DisplayName);
    }

    [Fact]
    public void Validate_WithEmptyEmail_FailsValidation()
    {
        var command = new RegisterUserCommand("firebase-uid-123", "John Doe", "");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_WithInvalidEmail_FailsValidation()
    {
        var command = new RegisterUserCommand("firebase-uid-123", "John Doe", "not-an-email");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_WithLongEmail_FailsValidation()
    {
        var command = new RegisterUserCommand("firebase-uid-123", "John Doe", new string('a', 245) + "@example.com");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }
}
