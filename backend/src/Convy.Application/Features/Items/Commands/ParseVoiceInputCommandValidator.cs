using FluentValidation;

namespace Convy.Application.Features.Items.Commands;

public class ParseVoiceAudioCommandValidator : AbstractValidator<ParseVoiceAudioCommand>
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3", ".mp4", ".m4a", ".wav", ".webm", ".mpeg", ".mpga", ".ogg"
    };

    private const long MaxFileSizeBytes = 25 * 1024 * 1024; // 25MB OpenAI limit

    public ParseVoiceAudioCommandValidator()
    {
        RuleFor(x => x.ListId).NotEmpty();
        RuleFor(x => x.Audio).NotNull().WithMessage("Audio stream is required.");
        RuleFor(x => x.FileName)
            .NotEmpty()
            .Must(name => AllowedExtensions.Contains(Path.GetExtension(name)))
            .WithMessage("Audio format not supported. Supported formats: mp3, mp4, m4a, wav, webm, mpeg, mpga, ogg.");

        RuleFor(x => x.AudioLengthBytes)
            .Must((command, _) => GetAudioLength(command) <= MaxFileSizeBytes)
            .WithMessage("Audio file must not exceed 25 MB.");
    }

    private static long GetAudioLength(ParseVoiceAudioCommand command)
    {
        if (command.AudioLengthBytes.HasValue)
            return command.AudioLengthBytes.Value;

        return command.Audio is not null && command.Audio.CanSeek
            ? command.Audio.Length
            : 0;
    }
}
