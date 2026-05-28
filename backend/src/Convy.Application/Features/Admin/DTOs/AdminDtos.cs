using System.Text.Json.Serialization;

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
    int AiRequests7d,
    int AiSuccesses7d,
    int AiFailures7d,
    int VoiceItemsCreated7d,
    long? EstimatedAiCostMicros7d,
    AdminMcpSummaryDto Mcp,
    BackupRunDto? LastBackup,
    AdminSystemHealthDto Health);

public record AdminMcpSummaryDto(
    bool McpHealthy,
    bool AuthHealthy,
    int ToolCalls24h,
    int ToolSuccesses24h,
    int ToolFailures24h,
    double SuccessRate24h,
    DateTime? LastInvocationAt);

public record AdminUsageMetricsDto(
    DateOnly From,
    DateOnly To,
    IReadOnlyList<DailyUsageMetricDto> Days);

public record DailyUsageMetricDto(
    DateOnly Date,
    int HouseholdsActive,
    int ItemsCreated,
    int ItemsCompleted,
    int ItemsUncompleted,
    int ItemsDeleted,
    int ItemCompletionsCreatedSameDay,
    int ItemCompletionsFromBacklog,
    int TasksCreated,
    int TasksCompleted,
    int TasksUncompleted,
    int TasksDeleted);

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
    bool McpHealthy,
    bool AuthHealthy,
    bool McpMetadataHealthy,
    bool AuthMetadataHealthy,
    long? DiskFreeBytes,
    long? PostgresDataSizeBytes,
    string? BackendVersion,
    string? AndroidVersion,
    DateTime? LastDeployAt,
    string? ReleaseSha,
    string? OperatingSystem,
    string? Architecture,
    int ProcessorCount,
    string? CpuModel,
    long? MemoryTotalBytes,
    long? MemoryAvailableBytes,
    long? DiskTotalBytes,
    long? UptimeSeconds,
    double? LoadAverage1m);

public record AdminMcpOverviewDto(
    DateOnly From,
    DateOnly To,
    AdminMcpRuntimeDto Runtime,
    [property: JsonPropertyName("oauth")]
    AdminMcpOAuthMetricsDto OAuth,
    AdminMcpUsageMetricsDto Usage,
    IReadOnlyList<DailyMcpToolMetricDto> Days,
    IReadOnlyList<McpToolMetricDto> Tools,
    IReadOnlyList<McpRecentInvocationDto> RecentInvocations,
    IReadOnlyList<McpToolCatalogItemDto> ToolCatalog,
    IReadOnlyList<McpPublicationReadinessCheckDto> ReadinessChecks);

public record AdminMcpRuntimeDto(
    string McpUrl,
    string AuthUrl,
    string Issuer,
    string Audience,
    IReadOnlyList<string> Scopes,
    bool McpHealthHealthy,
    bool AuthHealthHealthy,
    bool McpMetadataHealthy,
    bool AuthMetadataHealthy);

public record AdminMcpOAuthMetricsDto(
    int ActiveConsents,
    int RevokedConsents,
    int ActiveRefreshTokens,
    int RevokedRefreshTokens,
    int RefreshTokensExpiring7d,
    DateTime? LastConsentAt,
    DateTime? LastTokenUsedAt,
    DateTime? LastRevokedAt);

public record AdminMcpUsageMetricsDto(
    int Invocations,
    int Successes,
    int Failures,
    int ValidationErrors,
    int Unauthorized,
    int Forbidden,
    int NotFound,
    int ProviderErrors,
    int UnexpectedErrors,
    double SuccessRate,
    double? AverageLatencyMs,
    long? P95LatencyMs,
    DateTime? LastInvocationAt);

public record DailyMcpToolMetricDto(
    DateOnly Date,
    int Invocations,
    int Successes,
    int Failures,
    double? AverageLatencyMs);

public record McpToolMetricDto(
    string ToolName,
    int Invocations,
    int Successes,
    int Failures,
    double? AverageLatencyMs,
    long? P95LatencyMs,
    DateTime? LastInvocationAt);

public record McpRecentInvocationDto(
    DateTime CreatedAt,
    string ToolName,
    string Status,
    long LatencyMs,
    string? ErrorType,
    string UserId,
    string? HouseholdId);

public record McpToolCatalogItemDto(
    string Name,
    string Title,
    IReadOnlyList<string> RequiredScopes,
    bool ReadOnlyHint,
    bool DestructiveHint,
    bool IdempotentHint,
    bool OpenWorldHint);

public record McpPublicationReadinessCheckDto(
    string Key,
    string Label,
    string Status,
    string Details);

public record AdminOpenAiMetricsDto(
    DateOnly From,
    DateOnly To,
    int Requests,
    int Successes,
    int Failures,
    int InputTokens,
    int OutputTokens,
    int CachedTokens,
    int ReasoningTokens,
    int AudioTokens,
    int TextTokens,
    double AudioDurationSeconds,
    long? EstimatedCostMicros,
    double? AverageLatencyMs,
    IReadOnlyList<DailyOpenAiMetricDto> Days,
    IReadOnlyList<OpenAiOperationMetricDto> Operations);

public record DailyOpenAiMetricDto(
    DateOnly Date,
    int Requests,
    int Successes,
    int Failures,
    int InputTokens,
    int OutputTokens,
    int CachedTokens,
    int ReasoningTokens,
    int AudioTokens,
    int TextTokens,
    double AudioDurationSeconds,
    long? EstimatedCostMicros,
    double? AverageLatencyMs);

public record OpenAiOperationMetricDto(
    string Feature,
    string Operation,
    string? Model,
    int Requests,
    int Successes,
    int Failures,
    int InputTokens,
    int OutputTokens,
    int CachedTokens,
    int ReasoningTokens,
    int AudioTokens,
    int TextTokens,
    double AudioDurationSeconds,
    long? EstimatedCostMicros,
    double? AverageLatencyMs);
