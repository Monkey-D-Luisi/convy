using Convy.Application.Features.Items.Commands;

namespace Convy.Application.Common.Interfaces;

public interface IAiVoiceParsingService
{
    Task<VoiceParsingResult> ParseAudioAsync(
        Stream audio,
        string fileName,
        Guid householdId,
        CancellationToken cancellationToken = default);
}

public record VoiceParsingResult(string Transcription, List<ParsedItemDto> Items);
