namespace Convy.Application.Common.Interfaces;

public interface IAdminBackupFileService
{
    Task<AdminBackupDownload?> OpenDownloadAsync(Guid backupRunId, CancellationToken cancellationToken = default);
}

public sealed record AdminBackupDownload(
    string FileName,
    string ContentType,
    long? SizeBytes,
    string? Sha256,
    Stream Content);
