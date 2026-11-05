using System.Data;
using Convy.Application.Features.Admin.DTOs;
using Convy.Domain.Entities;
using Convy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Convy.Infrastructure.Services;

internal sealed record SystemMetricSample(
    long? DiskFreeBytes,
    long? DiskTotalBytes,
    long? MemoryAvailableBytes,
    long? MemoryTotalBytes,
    double? LoadAverage1m,
    long? UptimeSeconds,
    long? PostgresDataSizeBytes,
    string MetricScope = "legacy-host",
    ContainerMetricsDto? Containers = null);

internal interface ISystemMetricSource
{
    Task<SystemMetricSample> CaptureAsync(CancellationToken cancellationToken = default);
}

internal sealed class SystemMetricSource : ISystemMetricSource
{
    private readonly ConvyDbContext _context;
    private readonly IConfiguration _configuration;

    public SystemMetricSource(ConvyDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<SystemMetricSample> CaptureAsync(CancellationToken cancellationToken = default) =>
        new(null, null, null, null, null, null,
            await GetPostgresDataSizeAsync(cancellationToken), "container",
            await ContainerMetricsFile.ReadAsync(
                _configuration["Operations:ContainerMetricsPath"] ?? ContainerMetricsFile.DefaultPath,
                DateTimeOffset.UtcNow, cancellationToken));

    private async Task<long?> GetPostgresDataSizeAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!_context.Database.IsRelational())
                return null;

            var connection = _context.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = "select pg_database_size(current_database())";
            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result is long value ? value : Convert.ToInt64(result);
        }
        catch
        {
            return null;
        }
    }
}

internal sealed class SystemMetricSnapshotRecorder
{
    public const int RetentionDays = 30;

    private readonly ConvyDbContext _context;
    private readonly ISystemMetricSource _source;

    public SystemMetricSnapshotRecorder(ConvyDbContext context, ISystemMetricSource source)
    {
        _context = context;
        _source = source;
    }

    public async Task RecordAsync(DateTime now, CancellationToken cancellationToken = default)
    {
        var sample = await _source.CaptureAsync(cancellationToken);
        var cutoff = now.AddDays(-RetentionDays);
        var oldSnapshots = await _context.SystemMetricSnapshots
            .Where(snapshot => snapshot.CapturedAt < cutoff)
            .ToListAsync(cancellationToken);

        _context.SystemMetricSnapshots.RemoveRange(oldSnapshots);
        _context.SystemMetricSnapshots.Add(new SystemMetricSnapshot(
            now,
            sample.DiskFreeBytes,
            sample.DiskTotalBytes,
            sample.MemoryAvailableBytes,
            sample.MemoryTotalBytes,
            sample.LoadAverage1m,
            sample.UptimeSeconds,
            sample.PostgresDataSizeBytes,
            sample.MetricScope,
            sample.Containers is null ? null : ContainerMetricsFile.Serialize(sample.Containers)));

        await _context.SaveChangesAsync(cancellationToken);
    }
}

internal sealed class SystemMetricSnapshotHostedService : BackgroundService
{
    private static readonly TimeSpan CaptureInterval = TimeSpan.FromMinutes(15);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SystemMetricSnapshotHostedService> _logger;

    public SystemMetricSnapshotHostedService(IServiceScopeFactory scopeFactory, ILogger<SystemMetricSnapshotHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var recorder = scope.ServiceProvider.GetRequiredService<SystemMetricSnapshotRecorder>();
                await recorder.RecordAsync(DateTime.UtcNow, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording system metric snapshot");
            }

            await Task.Delay(CaptureInterval, stoppingToken);
        }
    }
}
