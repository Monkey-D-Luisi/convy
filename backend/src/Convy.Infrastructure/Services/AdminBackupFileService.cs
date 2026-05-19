using Convy.Application.Common.Interfaces;
using Convy.Domain.ValueObjects;
using Convy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Convy.Infrastructure.Services;

public class AdminBackupFileService : IAdminBackupFileService
{
    private readonly ConvyDbContext _context;
    private readonly IConfiguration _configuration;

    public AdminBackupFileService(ConvyDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<AdminBackupDownload?> OpenDownloadAsync(Guid backupRunId, CancellationToken cancellationToken = default)
    {
        var run = await _context.BackupRuns.AsNoTracking().FirstOrDefaultAsync(b => b.Id == backupRunId, cancellationToken);
        if (run is null || run.Status != BackupRunStatus.Success || string.IsNullOrWhiteSpace(run.FileName))
            return null;

        if (run.FileName.IndexOfAny([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]) >= 0)
            return null;

        var root = Path.GetFullPath(_configuration["Operations:BackupRoot"] ?? "/opt/convy/backups/postgres");
        var typeDirectory = run.BackupType.ToString().ToLowerInvariant();
        var path = Path.GetFullPath(Path.Combine(root, typeDirectory, run.FileName));
        var rootWithSeparator = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;
        if (!path.StartsWith(rootWithSeparator, StringComparison.Ordinal))
            return null;

        if (!File.Exists(path))
            return null;

        var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return new AdminBackupDownload(
            run.FileName,
            "application/octet-stream",
            run.SizeBytes,
            run.Sha256,
            stream);
    }
}
