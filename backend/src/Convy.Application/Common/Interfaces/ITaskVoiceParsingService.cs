using Convy.Application.Features.Tasks.Commands;

namespace Convy.Application.Common.Interfaces;

public interface ITaskVoiceParsingService
{
    Task<TaskVoiceParsingResult> ParseAudioAsync(
        Stream audio,
        string fileName,
        Guid householdId,
        IReadOnlyList<TaskVoiceHouseholdMember> householdMembers,
        string timeZoneId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);
}

public record TaskVoiceHouseholdMember(Guid UserId, string DisplayName);
