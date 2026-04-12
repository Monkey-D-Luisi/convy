using Convy.Application.Common.Interfaces;
using Convy.Application.Features.Items.Commands;
using Microsoft.Extensions.Logging;

namespace Convy.Infrastructure.Services;

public class NoOpVoiceParsingService : IAiVoiceParsingService
{
    private readonly ILogger<NoOpVoiceParsingService> _logger;

    public NoOpVoiceParsingService(ILogger<NoOpVoiceParsingService> logger)
    {
        _logger = logger;
    }

    public Task<VoiceParsingResult> ParseAudioAsync(
        Stream audio,
        string fileName,
        Guid householdId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Voice parsing requested but OPENAI_API_KEY is not configured. Returning empty result.");
        return Task.FromResult(new VoiceParsingResult(string.Empty, []));
    }
}
