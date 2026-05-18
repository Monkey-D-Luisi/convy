namespace Convy.Domain.ValueObjects;

public enum BackupRunStatus
{
    Success = 0,
    Failed = 1,
}

public enum BackupRunType
{
    Daily = 0,
    Weekly = 1,
    Monthly = 2,
    Manual = 3,
}

public enum BackupVerificationStatus
{
    NotRun = 0,
    PgRestoreListOk = 1,
    RestoreOk = 2,
    Failed = 3,
}
