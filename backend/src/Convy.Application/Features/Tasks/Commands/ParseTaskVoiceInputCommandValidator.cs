using FluentValidation;

namespace Convy.Application.Features.Tasks.Commands;

public class ParseTaskVoiceAudioCommandValidator : AbstractValidator<ParseTaskVoiceAudioCommand>
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3", ".mp4", ".m4a", ".wav", ".webm", ".mpeg", ".mpga", ".ogg"
    };

    public ParseTaskVoiceAudioCommandValidator()
    {
        RuleFor(x => x.ListId)
            .NotEmpty().WithMessage("List ID is required.");

        RuleFor(x => x.Audio)
            .NotNull().WithMessage("Audio stream is required.");

        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("Audio file name is required.")
            .Must(fileName => SupportedExtensions.Contains(Path.GetExtension(fileName)))
            .WithMessage("Audio format not supported. Supported formats: mp3, mp4, m4a, wav, webm, mpeg, mpga, ogg.");

        RuleFor(x => x.TimeZoneId)
            .NotEmpty().WithMessage("Time zone is required.")
            .MaximumLength(100).WithMessage("Time zone must not exceed 100 characters.");

        RuleFor(x => x.AudioLengthBytes)
            .LessThanOrEqualTo(25 * 1024 * 1024).When(x => x.AudioLengthBytes.HasValue)
            .WithMessage("Audio file must not exceed 25 MB.");
    }
}
