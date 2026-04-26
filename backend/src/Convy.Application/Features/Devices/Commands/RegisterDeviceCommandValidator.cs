using FluentValidation;

namespace Convy.Application.Features.Devices.Commands;

public class RegisterDeviceCommandValidator : AbstractValidator<RegisterDeviceCommand>
{
    public RegisterDeviceCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Platform).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Locale).MaximumLength(20);
    }
}
