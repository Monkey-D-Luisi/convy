using Convy.Domain.Entities;

namespace Convy.Domain.Repositories;

public interface IBackupRunRepository
{
    Task AddAsync(BackupRun backupRun, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
