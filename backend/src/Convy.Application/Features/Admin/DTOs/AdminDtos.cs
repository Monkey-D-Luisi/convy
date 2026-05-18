namespace Convy.Application.Features.Admin.DTOs;

public record AdminOverviewDto(
    int UsersTotal,
    int HouseholdsTotal,
    int HouseholdsActive7d,
    int HouseholdsActive30d,
    int ListsTotal,
    int ItemsCreated7d,
    int ItemsCompleted7d,
    int TasksCreated7d,
    int TasksCompleted7d,
    int VoiceParseRequests7d,
    int VoiceParseSuccess7d,
    int VoiceParseFailures7d,
    int VoiceItemsCreated7d,
    long? EstimatedOpenAiCostMicros7d,
    BackupRunDto? LastBackup,
    AdminSystemHealthDto Health);

public record AdminUsageMetricsDto(
    DateOnly From,
    DateOnly To,
    IReadOnlyList<DailyUsageMetricDto> Days);

public record DailyUsageMetricDto(
    DateOnly Date,
    int HouseholdsActive,
    int ItemsCreated,
    int ItemsCompleted,
    int TasksCreated,
    int TasksCompleted);

public record AdminVoiceMetricsDto(
    DateOnly From,
    DateOnly To,
    int Requests,
    int Successes,
    int Failures,
    int ParsedItems,
    int VoiceItemsCreated,
    int InputTokens,
    int OutputTokens,
    int CachedTokens,
    int ReasoningTokens,
    long? EstimatedCostMicros,
    IReadOnlyList<DailyVoiceMetricDto> Days);

public record DailyVoiceMetricDto(
    DateOnly Date,
    int Requests,
    int Successes,
    int Failures,
    int ParsedItems,
    int VoiceItemsCreated,
    int InputTokens,
    int OutputTokens,
    int CachedTokens,
    int ReasoningTokens,
    long? EstimatedCostMicros);

public record BackupRunDto(
    Guid Id,
    string Status,
    string BackupType,
    string? FileName,
    long? SizeBytes,
    string? Sha256,
    long DurationMs,
    string VerificationStatus,
    string? ErrorMessage,
    DateTime StartedAt,
    DateTime FinishedAt);

public record AdminSystemHealthDto(
    bool ApiHealthy,
    bool DatabaseHealthy,
    long? DiskFreeBytes,
    long? PostgresDataSizeBytes,
    string? BackendVersion,
    string? AndroidVersion,
    DateTime? LastDeployAt);
