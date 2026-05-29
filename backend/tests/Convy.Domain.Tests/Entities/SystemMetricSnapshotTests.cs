using Convy.Domain.Entities;
using FluentAssertions;

namespace Convy.Domain.Tests.Entities;

public class SystemMetricSnapshotTests
{
    [Fact]
    public void Constructor_WithValidMetrics_CreatesSnapshot()
    {
        var capturedAt = new DateTime(2026, 5, 29, 12, 0, 0, DateTimeKind.Utc);

        var snapshot = new SystemMetricSnapshot(
            capturedAt,
            diskFreeBytes: 10,
            diskTotalBytes: 20,
            memoryAvailableBytes: 30,
            memoryTotalBytes: 40,
            loadAverage1m: 0.25,
            uptimeSeconds: 50,
            postgresDataSizeBytes: 60);

        snapshot.CapturedAt.Should().Be(capturedAt);
        snapshot.DiskFreeBytes.Should().Be(10);
        snapshot.MemoryAvailableBytes.Should().Be(30);
        snapshot.LoadAverage1m.Should().Be(0.25);
    }

    [Fact]
    public void Constructor_WithNegativeMetric_ThrowsArgumentException()
    {
        var act = () => new SystemMetricSnapshot(
            DateTime.UtcNow,
            diskFreeBytes: -1,
            diskTotalBytes: 20,
            memoryAvailableBytes: 30,
            memoryTotalBytes: 40,
            loadAverage1m: 0.25,
            uptimeSeconds: 50,
            postgresDataSizeBytes: 60);

        act.Should().Throw<ArgumentException>();
    }
}
