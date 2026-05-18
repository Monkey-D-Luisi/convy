using Convy.Domain.Entities;
using Convy.Domain.ValueObjects;
using FluentAssertions;

namespace Convy.Domain.Tests.Entities;

public class BackupRunTests
{
    [Fact]
    public void Constructor_WithSuccessfulBackup_CreatesRun()
    {
        var startedAt = DateTime.UtcNow.AddSeconds(-10);
        var finishedAt = DateTime.UtcNow;

        var run = new BackupRun(
            BackupRunStatus.Success,
            BackupRunType.Daily,
            "convy_20260518_030000.dump",
            sizeBytes: 1024,
            sha256: new string('a', 64),
            durationMs: 10_000,
            BackupVerificationStatus.PgRestoreListOk,
            errorMessage: null,
            startedAt,
            finishedAt);

        run.Status.Should().Be(BackupRunStatus.Success);
        run.BackupType.Should().Be(BackupRunType.Daily);
        run.FileName.Should().Be("convy_20260518_030000.dump");
        run.VerificationStatus.Should().Be(BackupVerificationStatus.PgRestoreListOk);
        run.FinishedAt.Should().Be(finishedAt);
    }

    [Fact]
    public void Constructor_WithFinishedBeforeStarted_ThrowsArgumentException()
    {
        var startedAt = DateTime.UtcNow;
        var finishedAt = startedAt.AddSeconds(-1);

        var act = () => new BackupRun(
            BackupRunStatus.Failed,
            BackupRunType.Daily,
            null,
            null,
            null,
            1,
            BackupVerificationStatus.Failed,
            "failed",
            startedAt,
            finishedAt);

        act.Should().Throw<ArgumentException>();
    }
}
