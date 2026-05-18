using Convy.Domain.Common;
using Convy.Domain.ValueObjects;

namespace Convy.Domain.Entities;

public class BackupRun : Entity
{
    public BackupRunStatus Status { get; private set; }
    public BackupRunType BackupType { get; private set; }
    public string? FileName { get; private set; }
    public long? SizeBytes { get; private set; }
    public string? Sha256 { get; private set; }
    public long DurationMs { get; private set; }
    public BackupVerificationStatus VerificationStatus { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime FinishedAt { get; private set; }

    private BackupRun() { }

    public BackupRun(
        BackupRunStatus status,
        BackupRunType backupType,
        string? fileName,
        long? sizeBytes,
        string? sha256,
        long durationMs,
        BackupVerificationStatus verificationStatus,
        string? errorMessage,
        DateTime startedAt,
        DateTime finishedAt)
    {
        if (sizeBytes is < 0)
            throw new ArgumentException("Backup size must not be negative.", nameof(sizeBytes));
        if (durationMs < 0)
            throw new ArgumentException("Backup duration must not be negative.", nameof(durationMs));
        if (finishedAt < startedAt)
            throw new ArgumentException("Finished time must be after started time.", nameof(finishedAt));
        if (sha256 is not null && sha256.Length != 64)
            throw new ArgumentException("SHA256 must be 64 hexadecimal characters.", nameof(sha256));

        Status = status;
        BackupType = backupType;
        FileName = string.IsNullOrWhiteSpace(fileName) ? null : fileName.Trim();
        SizeBytes = sizeBytes;
        Sha256 = string.IsNullOrWhiteSpace(sha256) ? null : sha256.Trim();
        DurationMs = durationMs;
        VerificationStatus = verificationStatus;
        ErrorMessage = string.IsNullOrWhiteSpace(errorMessage) ? null : errorMessage.Trim();
        StartedAt = startedAt;
        FinishedAt = finishedAt;
    }
}
