using FluentValidation;

namespace Convy.Application.Features.Items.Commands;

public class ParseVoiceInputCommandValidator : AbstractValidator<ParseVoiceInputCommand>
{
    public ParseVoiceInputCommandValidator()
    {
        RuleFor(x => x.ListId).NotEmpty();
        RuleFor(x => x.TranscribedText).NotEmpty().MaximumLength(2000);
    }
}
