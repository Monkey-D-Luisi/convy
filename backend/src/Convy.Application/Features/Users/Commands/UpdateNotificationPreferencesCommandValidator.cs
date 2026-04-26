using FluentValidation;

namespace Convy.Application.Features.Users.Commands;

public class UpdateNotificationPreferencesCommandValidator : AbstractValidator<UpdateNotificationPreferencesCommand>
{
    public UpdateNotificationPreferencesCommandValidator()
    {
    }
}
