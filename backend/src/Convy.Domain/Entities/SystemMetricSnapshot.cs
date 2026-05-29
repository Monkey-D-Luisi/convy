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

    private SystemMetricSnapshot() { }

    public SystemMetricSnapshot(
        DateTime capturedAt,
        long? diskFreeBytes,
        long? diskTotalBytes,
        long? memoryAvailableBytes,
        long? memoryTotalBytes,
        double? loadAverage1m,
        long? uptimeSeconds,
        long? postgresDataSizeBytes)
    {
        ThrowIfNegative(diskFreeBytes, nameof(diskFreeBytes));
        ThrowIfNegative(diskTotalBytes, nameof(diskTotalBytes));
        ThrowIfNegative(memoryAvailableBytes, nameof(memoryAvailableBytes));
        ThrowIfNegative(memoryTotalBytes, nameof(memoryTotalBytes));
        ThrowIfNegative(loadAverage1m, nameof(loadAverage1m));
        ThrowIfNegative(uptimeSeconds, nameof(uptimeSeconds));
        ThrowIfNegative(postgresDataSizeBytes, nameof(postgresDataSizeBytes));

        CapturedAt = capturedAt;
        DiskFreeBytes = diskFreeBytes;
        DiskTotalBytes = diskTotalBytes;
        MemoryAvailableBytes = memoryAvailableBytes;
        MemoryTotalBytes = memoryTotalBytes;
        LoadAverage1m = loadAverage1m;
        UptimeSeconds = uptimeSeconds;
        PostgresDataSizeBytes = postgresDataSizeBytes;
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
