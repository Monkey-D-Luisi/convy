using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using Convy.Infrastructure.Persistence;

namespace Convy.Infrastructure.Repositories;

public class BackupRunRepository : IBackupRunRepository
{
    private readonly ConvyDbContext _context;

    public BackupRunRepository(ConvyDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(BackupRun backupRun, CancellationToken cancellationToken = default)
    {
        await _context.BackupRuns.AddAsync(backupRun, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
