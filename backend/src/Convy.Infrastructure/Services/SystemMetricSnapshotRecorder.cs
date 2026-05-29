using System.Data;
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
    long? PostgresDataSizeBytes);

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
        new(
            DiskFreeBytes: GetDiskFreeBytes(),
            DiskTotalBytes: GetDiskTotalBytes(),
            MemoryAvailableBytes: GetMemoryValueBytes("MemAvailable:"),
            MemoryTotalBytes: GetMemoryValueBytes("MemTotal:"),
            LoadAverage1m: GetLoadAverage1m(),
            UptimeSeconds: GetUptimeSeconds(),
            PostgresDataSizeBytes: await GetPostgresDataSizeAsync(cancellationToken));

    private long? GetDiskFreeBytes()
    {
        try
        {
            var root = GetDataRoot();
            return string.IsNullOrWhiteSpace(root) ? null : new DriveInfo(root).AvailableFreeSpace;
        }
        catch
        {
            return null;
        }
    }

    private long? GetDiskTotalBytes()
    {
        try
        {
            var root = GetDataRoot();
            return string.IsNullOrWhiteSpace(root) ? null : new DriveInfo(root).TotalSize;
        }
        catch
        {
            return null;
        }
    }

    private string? GetDataRoot()
    {
        var path = _configuration["Operations:DataPath"] ?? "/opt/convy";
        return Path.GetPathRoot(path);
    }

    private static long? GetMemoryValueBytes(string key)
    {
        try
        {
            const string memInfoPath = "/proc/meminfo";
            if (!File.Exists(memInfoPath))
                return null;

            var line = File.ReadLines(memInfoPath).FirstOrDefault(value => value.StartsWith(key, StringComparison.Ordinal));
            if (line is null)
                return null;

            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 2 && long.TryParse(parts[1], out var kib) ? kib * 1024 : null;
        }
        catch
        {
            return null;
        }
    }

    private static long? GetUptimeSeconds()
    {
        try
        {
            const string uptimePath = "/proc/uptime";
            if (!File.Exists(uptimePath))
                return null;

            var first = File.ReadAllText(uptimePath).Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            return double.TryParse(first, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var uptime)
                ? (long)Math.Round(uptime)
                : null;
        }
        catch
        {
            return null;
        }
    }

    private static double? GetLoadAverage1m()
    {
        try
        {
            const string loadAveragePath = "/proc/loadavg";
            if (!File.Exists(loadAveragePath))
                return null;

            var first = File.ReadAllText(loadAveragePath).Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            return double.TryParse(first, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var load)
                ? load
                : null;
        }
        catch
        {
            return null;
        }
    }

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
            sample.PostgresDataSizeBytes));

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
