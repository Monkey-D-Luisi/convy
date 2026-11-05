using Convy.Application.Features.Admin.DTOs;
using Convy.Domain.Entities;
using Convy.Infrastructure.Persistence;
using Convy.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Convy.Infrastructure.Tests.Services;

public class ContainerMetricsTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 4, 12, 0, 0, TimeSpan.Zero);

    private static ContainerMetricsDto Sample() => new(1, "container", "convy", Now,
        ContainerMetricsFile.Unknown().Services.Select((service, index) => service with
        {
            ContainerId = index.ToString("x64"), State = "running", Health = "unknown", CpuPercent = 1.25,
            MemoryBytes = 1024, RestartCount = 0, StartedAtUtc = Now.AddHours(-1),
        }).ToArray());

    [Fact]
    public async Task ReadAsync_ValidProductFile_ReturnsExactSixContainerMeasurements()
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, ContainerMetricsFile.Serialize(Sample()));
            var result = await ContainerMetricsFile.ReadAsync(path, Now);
            result.Status.Should().Be("available");
            result.Services.Should().HaveCount(6);
            result.Services.Should().OnlyContain(service => service.MemoryBytes == 1024 && service.Health == "unknown");
        }
        finally { File.Delete(path); }
    }

    [Theory]
    [InlineData("product")]
    [InlineData("scope")]
    [InlineData("duplicate-name")]
    [InlineData("duplicate-id")]
    [InlineData("wrong-container")]
    [InlineData("negative")]
    [InlineData("future")]
    [InlineData("stale")]
    [InlineData("null-service")]
    public void Parse_InvalidOwnershipIdentityOrFreshness_ReportsUnavailable(string scenario)
    {
        var sample = Sample();
        var services = sample.Services.ToArray();
        sample = scenario switch
        {
            "product" => sample with { Product = "converso" },
            "scope" => sample with { Scope = "host" },
            "future" => sample with { CapturedAtUtc = Now.AddMinutes(2) },
            "stale" => sample with { CapturedAtUtc = Now.AddMinutes(-4) },
            _ => sample,
        };
        services[0] = scenario switch
        {
            "duplicate-name" => services[1] with { ContainerId = services[0].ContainerId },
            "duplicate-id" => services[0] with { ContainerId = services[1].ContainerId },
            "wrong-container" => services[0] with { ContainerName = "convy-caddy" },
            "negative" => services[0] with { MemoryBytes = -1 },
            "null-service" => null!,
            _ => services[0],
        };
        sample = sample with { Services = services };
        var result = ContainerMetricsFile.Parse(ContainerMetricsFile.Serialize(sample), Now);
        result.Status.Should().NotBe("available");
        result.Services.Should().OnlyContain(s => s.MemoryBytes == null && s.CpuPercent == null);
    }

    [Fact]
    public async Task ReadAsync_MissingMalformedOrOversizedFile_ReturnsUnknown()
    {
        var path = Path.GetTempFileName();
        try
        {
            foreach (var value in new[] { "{bad", new string('x', 16385), "{}" })
            {
                await File.WriteAllTextAsync(path, value);
                (await ContainerMetricsFile.ReadAsync(path, Now)).Status.Should().Be("unknown");
            }
            File.Delete(path);
            (await ContainerMetricsFile.ReadAsync(path, Now)).Status.Should().Be("unknown");
            (await ContainerMetricsFile.ReadAsync("", Now)).Status.Should().Be("unknown");
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task History_MixedScopes_PreservesLegacyValuesAndContainerIdentity()
    {
        await using var context = new ConvyDbContext(new DbContextOptionsBuilder<ConvyDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        context.SystemMetricSnapshots.Add(new SystemMetricSnapshot(Now.UtcDateTime.AddMinutes(-15), 10, 20, 30, 40, 1, 100, 50));
        context.SystemMetricSnapshots.Add(new SystemMetricSnapshot(Now.UtcDateTime, null, null, null, null, null, null, 60,
            "container", ContainerMetricsFile.Serialize(Sample())));
        await context.SaveChangesAsync();
        var reader = new AdminMetricsReader(context, new ConfigurationBuilder().Build());
        var history = await reader.GetSystemHistoryAsync(new DateOnly(2026, 11, 4), new DateOnly(2026, 11, 4));
        history.Samples[0].MetricScope.Should().Be("legacy-host");
        history.Samples[0].MemoryTotalBytes.Should().Be(40);
        history.Samples[0].Containers.Should().BeNull();
        history.Samples[1].MetricScope.Should().Be("container");
        history.Samples[1].MemoryTotalBytes.Should().BeNull();
        history.Samples[1].Containers!.Services[0].ContainerId.Should().Be(Sample().Services[0].ContainerId);
    }

    [Fact]
    public async Task RealSource_WithoutTelemetry_DoesNotSampleHostProcOrFilesystem()
    {
        await using var context = new ConvyDbContext(new DbContextOptionsBuilder<ConvyDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var source = new SystemMetricSource(context, new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["Operations:ContainerMetricsPath"] = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()) }).Build());
        var sample = await source.CaptureAsync();
        sample.MetricScope.Should().Be("container");
        sample.MemoryTotalBytes.Should().BeNull();
        sample.DiskTotalBytes.Should().BeNull();
        sample.UptimeSeconds.Should().BeNull();
        sample.LoadAverage1m.Should().BeNull();
        sample.Containers!.Status.Should().Be("unknown");
        await new SystemMetricSnapshotRecorder(context, source).RecordAsync(Now.UtcDateTime);
        context.SystemMetricSnapshots.Single().MetricScope.Should().Be("container");
    }
}
