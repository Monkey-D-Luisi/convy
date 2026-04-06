using Convy.Application.Features.Devices.Commands;
using FluentValidation.TestHelper;

namespace Convy.Application.Tests.Features.Devices;

public class RegisterDeviceCommandValidatorTests
{
    private readonly RegisterDeviceCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidData_PassesValidation()
    {
        var command = new RegisterDeviceCommand("fcm-token-123", "android");
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyToken_FailsValidation()
    {
        var command = new RegisterDeviceCommand("", "android");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Token);
    }

    [Fact]
    public void Validate_WithEmptyPlatform_FailsValidation()
    {
        var command = new RegisterDeviceCommand("fcm-token-123", "");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Platform);
    }

    [Fact]
    public void Validate_WithLongToken_FailsValidation()
    {
        var command = new RegisterDeviceCommand(new string('a', 501), "android");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Token);
    }

    [Fact]
    public void Validate_WithLongPlatform_FailsValidation()
    {
        var command = new RegisterDeviceCommand("fcm-token-123", new string('a', 21));
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Platform);
    }
}
