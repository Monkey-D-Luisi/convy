using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using Convy.Infrastructure.Persistence;

namespace Convy.Infrastructure.Repositories;

public class VoiceParseEventRepository : IVoiceParseEventRepository
{
    private readonly ConvyDbContext _context;

    public VoiceParseEventRepository(ConvyDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(VoiceParseEvent voiceParseEvent, CancellationToken cancellationToken = default)
    {
        await _context.VoiceParseEvents.AddAsync(voiceParseEvent, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
