using Convy.Application.Common.Interfaces;
using Convy.Application.Features.Tasks.Commands;
using Microsoft.Extensions.Logging;

namespace Convy.Infrastructure.Services;

public class NoOpTaskVoiceParsingService : ITaskVoiceParsingService
{
    private readonly ILogger<NoOpTaskVoiceParsingService> _logger;

    public NoOpTaskVoiceParsingService(ILogger<NoOpTaskVoiceParsingService> logger)
    {
        _logger = logger;
    }

    public Task<TaskVoiceParsingResult> ParseAudioAsync(
        Stream audio,
        string fileName,
        Guid householdId,
        IReadOnlyList<TaskVoiceHouseholdMember> householdMembers,
        string timeZoneId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Task voice parsing requested but OPENAI_API_KEY is not configured. Returning empty result.");
        return Task.FromResult(new TaskVoiceParsingResult(string.Empty, []));
    }
}
