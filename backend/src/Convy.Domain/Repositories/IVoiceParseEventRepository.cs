using Convy.Domain.Entities;

namespace Convy.Domain.Repositories;

public interface IVoiceParseEventRepository
{
    Task AddAsync(VoiceParseEvent voiceParseEvent, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
