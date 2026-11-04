using Convy.Domain.Common;

namespace Convy.Domain.Entities;

public class SystemMetricSnapshot : Entity
{
    public DateTime CapturedAt { get; private set; }
    public long? DiskFreeBytes { get; private set; }
    public long? DiskTotalBytes { get; private set; }
    public long? MemoryAvailableBytes { get; private set; }
    public long? MemoryTotalBytes { get; private set; }
    public double? LoadAverage1m { get; private set; }
    public long? UptimeSeconds { get; private set; }
    public long? PostgresDataSizeBytes { get; private set; }
    public string MetricScope { get; private set; } = "legacy-host";
    public string? ContainerMetricsJson { get; private set; }

    private SystemMetricSnapshot() { }

    public SystemMetricSnapshot(
        DateTime capturedAt,
        long? diskFreeBytes,
        long? diskTotalBytes,
        long? memoryAvailableBytes,
        long? memoryTotalBytes,
        double? loadAverage1m,
        long? uptimeSeconds,
        long? postgresDataSizeBytes,
        string metricScope = "legacy-host",
        string? containerMetricsJson = null)
    {
        ThrowIfNegative(diskFreeBytes, nameof(diskFreeBytes));
        ThrowIfNegative(diskTotalBytes, nameof(diskTotalBytes));
        ThrowIfNegative(memoryAvailableBytes, nameof(memoryAvailableBytes));
        ThrowIfNegative(memoryTotalBytes, nameof(memoryTotalBytes));
        ThrowIfNegative(loadAverage1m, nameof(loadAverage1m));
        ThrowIfNegative(uptimeSeconds, nameof(uptimeSeconds));
        ThrowIfNegative(postgresDataSizeBytes, nameof(postgresDataSizeBytes));
        if (metricScope is not ("legacy-host" or "container"))
            throw new ArgumentException("Unknown metric scope.", nameof(metricScope));
        if (metricScope == "legacy-host" && containerMetricsJson is not null || containerMetricsJson?.Length > 16384)
            throw new ArgumentException("Invalid container metric payload.", nameof(containerMetricsJson));
        if (metricScope == "container" && (diskFreeBytes is not null || diskTotalBytes is not null ||
            memoryAvailableBytes is not null || memoryTotalBytes is not null || loadAverage1m is not null || uptimeSeconds is not null))
            throw new ArgumentException("Container samples cannot contain host resource values.", nameof(metricScope));

        CapturedAt = capturedAt;
        DiskFreeBytes = diskFreeBytes;
        DiskTotalBytes = diskTotalBytes;
        MemoryAvailableBytes = memoryAvailableBytes;
        MemoryTotalBytes = memoryTotalBytes;
        LoadAverage1m = loadAverage1m;
        UptimeSeconds = uptimeSeconds;
        PostgresDataSizeBytes = postgresDataSizeBytes;
        MetricScope = metricScope;
        ContainerMetricsJson = containerMetricsJson;
    }

    private static void ThrowIfNegative(long? value, string parameterName)
    {
        if (value is < 0)
            throw new ArgumentException("Metric value must not be negative.", parameterName);
    }

    private static void ThrowIfNegative(double? value, string parameterName)
    {
        if (value is < 0)
            throw new ArgumentException("Metric value must not be negative.", parameterName);
    }
}
