using Convy.Application.Common.Interfaces;
using Convy.Application.Features.Items.Commands;

namespace Convy.Infrastructure.Services;

public class NoOpVoiceParsingService : IAiVoiceParsingService
{
    public Task<VoiceParsingResult> ParseAudioAsync(
        Stream audio,
        string fileName,
        Guid householdId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new VoiceParsingResult(string.Empty, []));
    }
}
