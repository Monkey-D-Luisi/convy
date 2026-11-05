namespace Convy.Application.Features.Admin.DTOs;

public record ContainerMetricsDto(
    int SchemaVersion,
    string Scope,
    string Product,
    DateTimeOffset CapturedAtUtc,
    IReadOnlyList<ContainerServiceMetricDto> Services,
    string Status = "available");

public record ContainerServiceMetricDto(
    string ContainerName,
    string? ContainerId,
    string State,
    string Health,
    double? CpuPercent,
    long? MemoryBytes,
    int? RestartCount,
    DateTimeOffset? StartedAtUtc);
