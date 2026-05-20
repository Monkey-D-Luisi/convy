using System.Data;
using System.Reflection;
using System.Runtime.InteropServices;
using Convy.Application.Common.Interfaces;
using Convy.Application.Features.Admin.DTOs;
using Convy.Domain.ValueObjects;
using Convy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Convy.Infrastructure.Services;

public class AdminMetricsReader : IAdminMetricsReader
{
    private readonly ConvyDbContext _context;
    private readonly IConfiguration _configuration;

    public AdminMetricsReader(ConvyDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<AdminOverviewDto> GetOverviewAsync(DateTime now, CancellationToken cancellationToken = default)
    {
        var since7d = now.AddDays(-7);
        var since30d = now.AddDays(-30);

        var usersTotal = await _context.Users.CountAsync(cancellationToken);
        var householdsTotal = await _context.Households.CountAsync(cancellationToken);
        var householdsActive7d = await _context.ActivityLogs
            .Where(a => a.CreatedAt >= since7d)
            .Select(a => a.HouseholdId)
            .Distinct()
            .CountAsync(cancellationToken);
        var householdsActive30d = await _context.ActivityLogs
            .Where(a => a.CreatedAt >= since30d)
            .Select(a => a.HouseholdId)
            .Distinct()
            .CountAsync(cancellationToken);
        var listsTotal = await _context.HouseholdLists.CountAsync(cancellationToken);
        var itemsCreated7d = await _context.ActivityLogs.CountAsync(a => a.EntityType == ActivityEntityType.Item && a.ActionType == ActivityActionType.Created && a.CreatedAt >= since7d, cancellationToken);
        var itemsCompleted7d = await _context.ActivityLogs.CountAsync(a => a.EntityType == ActivityEntityType.Item && a.ActionType == ActivityActionType.Completed && a.CreatedAt >= since7d, cancellationToken);
        var tasksCreated7d = await _context.ActivityLogs.CountAsync(a => a.EntityType == ActivityEntityType.Task && a.ActionType == ActivityActionType.Created && a.CreatedAt >= since7d, cancellationToken);
        var tasksCompleted7d = await _context.ActivityLogs.CountAsync(a => a.EntityType == ActivityEntityType.Task && a.ActionType == ActivityActionType.Completed && a.CreatedAt >= since7d, cancellationToken);
        var aiUsageEvents7d = await _context.AiUsageEvents
            .AsNoTracking()
            .Where(e => e.CreatedAt >= since7d)
            .ToListAsync(cancellationToken);
        var voiceItemsCreated7d = await _context.ListItems
            .CountAsync(i => i.Source == ItemCreationSource.Voice && i.CreatedAt >= since7d, cancellationToken);
        var lastBackup = await GetLatestBackupAsync(cancellationToken);
        var health = await GetSystemHealthAsync(cancellationToken);

        return new AdminOverviewDto(
            usersTotal,
            householdsTotal,
            householdsActive7d,
            householdsActive30d,
            listsTotal,
            itemsCreated7d,
            itemsCompleted7d,
            tasksCreated7d,
            tasksCompleted7d,
            aiUsageEvents7d.Count,
            aiUsageEvents7d.Count(e => e.Status == AiUsageStatus.Success),
            aiUsageEvents7d.Count(e => e.Status == AiUsageStatus.Failure),
            voiceItemsCreated7d,
            SumNullable(aiUsageEvents7d.Select(e => e.EstimatedCostMicros)),
            lastBackup,
            health);
    }

    public async Task<AdminUsageMetricsDto> GetUsageAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        var start = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var end = to.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var days = CreateDayRange(from, to);

        var activity = await _context.ActivityLogs
            .Where(a => a.CreatedAt >= start && a.CreatedAt < end)
            .Select(a => new { a.HouseholdId, a.CreatedAt })
            .ToListAsync(cancellationToken);
        var itemLogs = await _context.ActivityLogs
            .Where(a => a.EntityType == ActivityEntityType.Item)
            .Where(a => a.CreatedAt < end)
            .Where(a => a.CreatedAt >= start || a.ActionType == ActivityActionType.Created)
            .Select(a => new { a.EntityId, a.ActionType, a.CreatedAt })
            .ToListAsync(cancellationToken);

        var taskLogs = await _context.ActivityLogs
            .Where(a => a.EntityType == ActivityEntityType.Task)
            .Where(a => a.CreatedAt < end)
            .Where(a => a.CreatedAt >= start || a.ActionType == ActivityActionType.Created)
            .Select(a => new { a.EntityId, a.ActionType, a.CreatedAt })
            .ToListAsync(cancellationToken);

        var itemCreatedDates = itemLogs
            .Where(a => a.ActionType == ActivityActionType.Created)
            .GroupBy(a => a.EntityId)
            .ToDictionary(group => group.Key, group => group.Min(a => DateOnly.FromDateTime(a.CreatedAt)));

        var metrics = days.Select(day => new DailyUsageMetricDto(
            day,
            activity.Where(a => DateOnly.FromDateTime(a.CreatedAt) == day).Select(a => a.HouseholdId).Distinct().Count(),
            itemLogs.Count(a => a.ActionType == ActivityActionType.Created && DateOnly.FromDateTime(a.CreatedAt) == day),
            itemLogs.Count(a => a.ActionType == ActivityActionType.Completed && DateOnly.FromDateTime(a.CreatedAt) == day),
            itemLogs.Count(a => a.ActionType == ActivityActionType.Uncompleted && DateOnly.FromDateTime(a.CreatedAt) == day),
            itemLogs.Count(a => a.ActionType == ActivityActionType.Deleted && DateOnly.FromDateTime(a.CreatedAt) == day),
            itemLogs.Count(a => a.ActionType == ActivityActionType.Completed
                && DateOnly.FromDateTime(a.CreatedAt) == day
                && itemCreatedDates.TryGetValue(a.EntityId, out var createdDay)
                && createdDay == day),
            itemLogs.Count(a => a.ActionType == ActivityActionType.Completed
                && DateOnly.FromDateTime(a.CreatedAt) == day
                && (!itemCreatedDates.TryGetValue(a.EntityId, out var createdDay) || createdDay < day)),
            taskLogs.Count(a => a.ActionType == ActivityActionType.Created && DateOnly.FromDateTime(a.CreatedAt) == day),
            taskLogs.Count(a => a.ActionType == ActivityActionType.Completed && DateOnly.FromDateTime(a.CreatedAt) == day),
            taskLogs.Count(a => a.ActionType == ActivityActionType.Uncompleted && DateOnly.FromDateTime(a.CreatedAt) == day),
            taskLogs.Count(a => a.ActionType == ActivityActionType.Deleted && DateOnly.FromDateTime(a.CreatedAt) == day)))
            .ToList();

        return new AdminUsageMetricsDto(from, to, metrics);
    }

    public async Task<AdminVoiceMetricsDto> GetVoiceAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        var start = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var end = to.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var days = CreateDayRange(from, to);

        var events = await _context.VoiceParseEvents
            .Where(v => v.CreatedAt >= start && v.CreatedAt < end)
            .ToListAsync(cancellationToken);
        var voiceItems = await _context.ListItems
            .Where(i => i.Source == ItemCreationSource.Voice && i.CreatedAt >= start && i.CreatedAt < end)
            .Select(i => i.CreatedAt)
            .ToListAsync(cancellationToken);

        var daily = days.Select(day =>
        {
            var dayEvents = events.Where(v => DateOnly.FromDateTime(v.CreatedAt) == day).ToList();
            return new DailyVoiceMetricDto(
                day,
                dayEvents.Count,
                dayEvents.Count(v => v.Status == VoiceParseStatus.Success),
                dayEvents.Count(v => v.Status != VoiceParseStatus.Success),
                dayEvents.Sum(v => v.ParsedItemsCount),
                voiceItems.Count(d => DateOnly.FromDateTime(d) == day),
                dayEvents.Sum(v => v.InputTokens ?? 0),
                dayEvents.Sum(v => v.OutputTokens ?? 0),
                dayEvents.Sum(v => v.CachedTokens ?? 0),
                dayEvents.Sum(v => v.ReasoningTokens ?? 0),
                SumNullable(dayEvents.Select(v => v.EstimatedCostMicros)));
        }).ToList();

        return new AdminVoiceMetricsDto(
            from,
            to,
            events.Count,
            events.Count(v => v.Status == VoiceParseStatus.Success),
            events.Count(v => v.Status != VoiceParseStatus.Success),
            events.Sum(v => v.ParsedItemsCount),
            voiceItems.Count,
            events.Sum(v => v.InputTokens ?? 0),
            events.Sum(v => v.OutputTokens ?? 0),
            events.Sum(v => v.CachedTokens ?? 0),
            events.Sum(v => v.ReasoningTokens ?? 0),
            SumNullable(events.Select(v => v.EstimatedCostMicros)),
            daily);
    }

    public async Task<AdminOpenAiMetricsDto> GetOpenAiAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        var start = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var end = to.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var days = CreateDayRange(from, to);

        var events = await _context.AiUsageEvents
            .AsNoTracking()
            .Where(e => e.CreatedAt >= start && e.CreatedAt < end)
            .ToListAsync(cancellationToken);

        var daily = days.Select(day =>
        {
            var dayEvents = events.Where(e => DateOnly.FromDateTime(e.CreatedAt) == day).ToList();
            return new DailyOpenAiMetricDto(
                day,
                dayEvents.Count,
                dayEvents.Count(e => e.Status == Domain.ValueObjects.AiUsageStatus.Success),
                dayEvents.Count(e => e.Status == Domain.ValueObjects.AiUsageStatus.Failure),
                dayEvents.Sum(e => e.InputTokens ?? 0),
                dayEvents.Sum(e => e.OutputTokens ?? 0),
                dayEvents.Sum(e => e.CachedTokens ?? 0),
                dayEvents.Sum(e => e.ReasoningTokens ?? 0),
                dayEvents.Sum(e => e.AudioTokens ?? 0),
                dayEvents.Sum(e => e.TextTokens ?? 0),
                dayEvents.Sum(e => e.AudioDurationSeconds ?? 0),
                SumNullable(dayEvents.Select(e => e.EstimatedCostMicros)),
                AverageNullable(dayEvents.Select(e => (double?)e.LatencyMs)));
        }).ToList();

        var operations = events
            .GroupBy(e => new { e.Feature, e.Operation, e.Model })
            .OrderBy(group => group.Key.Feature)
            .ThenBy(group => group.Key.Operation)
            .ThenBy(group => group.Key.Model)
            .Select(group => new OpenAiOperationMetricDto(
                group.Key.Feature,
                group.Key.Operation,
                group.Key.Model,
                group.Count(),
                group.Count(e => e.Status == Domain.ValueObjects.AiUsageStatus.Success),
                group.Count(e => e.Status == Domain.ValueObjects.AiUsageStatus.Failure),
                group.Sum(e => e.InputTokens ?? 0),
                group.Sum(e => e.OutputTokens ?? 0),
                group.Sum(e => e.CachedTokens ?? 0),
                group.Sum(e => e.ReasoningTokens ?? 0),
                group.Sum(e => e.AudioTokens ?? 0),
                group.Sum(e => e.TextTokens ?? 0),
                group.Sum(e => e.AudioDurationSeconds ?? 0),
                SumNullable(group.Select(e => e.EstimatedCostMicros)),
                AverageNullable(group.Select(e => (double?)e.LatencyMs))))
            .ToList();

        return new AdminOpenAiMetricsDto(
            from,
            to,
            events.Count,
            events.Count(e => e.Status == Domain.ValueObjects.AiUsageStatus.Success),
            events.Count(e => e.Status == Domain.ValueObjects.AiUsageStatus.Failure),
            events.Sum(e => e.InputTokens ?? 0),
            events.Sum(e => e.OutputTokens ?? 0),
            events.Sum(e => e.CachedTokens ?? 0),
            events.Sum(e => e.ReasoningTokens ?? 0),
            events.Sum(e => e.AudioTokens ?? 0),
            events.Sum(e => e.TextTokens ?? 0),
            events.Sum(e => e.AudioDurationSeconds ?? 0),
            SumNullable(events.Select(e => e.EstimatedCostMicros)),
            AverageNullable(events.Select(e => (double?)e.LatencyMs)),
            daily,
            operations);
    }

    public async Task<BackupRunDto?> GetLatestBackupAsync(CancellationToken cancellationToken = default)
    {
        var run = await _context.BackupRuns
            .AsNoTracking()
            .OrderByDescending(b => b.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return run is null ? null : Map(run);
    }

    public async Task<IReadOnlyList<BackupRunDto>> GetBackupRunsAsync(int limit, CancellationToken cancellationToken = default)
    {
        var runs = await _context.BackupRuns
            .AsNoTracking()
            .OrderByDescending(b => b.StartedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return runs.Select(Map).ToList();
    }

    public async Task<AdminSystemHealthDto> GetSystemHealthAsync(CancellationToken cancellationToken = default)
    {
        var databaseHealthy = await _context.Database.CanConnectAsync(cancellationToken);

        return new AdminSystemHealthDto(
            ApiHealthy: true,
            DatabaseHealthy: databaseHealthy,
            DiskFreeBytes: GetDiskFreeBytes(),
            PostgresDataSizeBytes: databaseHealthy ? await GetPostgresDataSizeAsync(cancellationToken) : null,
            BackendVersion: _configuration["Backend:Version"] ?? Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
            AndroidVersion: _configuration["Mobile:AndroidVersion"],
            LastDeployAt: TryParseDateTime(_configuration["Deploy:LastDeployAt"]),
            ReleaseSha: _configuration["Deploy:ReleaseSha"],
            OperatingSystem: RuntimeInformation.OSDescription,
            Architecture: RuntimeInformation.OSArchitecture.ToString(),
            ProcessorCount: Environment.ProcessorCount,
            CpuModel: GetCpuModel(),
            MemoryTotalBytes: GetMemoryValueBytes("MemTotal:"),
            MemoryAvailableBytes: GetMemoryValueBytes("MemAvailable:"),
            DiskTotalBytes: GetDiskTotalBytes(),
            UptimeSeconds: GetUptimeSeconds(),
            LoadAverage1m: GetLoadAverage1m());
    }

    private static BackupRunDto Map(Domain.Entities.BackupRun run) => new(
        run.Id,
        run.Status.ToString(),
        run.BackupType.ToString(),
        run.FileName,
        run.SizeBytes,
        run.Sha256,
        run.DurationMs,
        run.VerificationStatus.ToString(),
        run.ErrorMessage,
        run.StartedAt,
        run.FinishedAt);

    private static IReadOnlyList<DateOnly> CreateDayRange(DateOnly from, DateOnly to)
    {
        var days = new List<DateOnly>();
        for (var date = from; date <= to; date = date.AddDays(1))
            days.Add(date);

        return days;
    }

    private static long? SumNullable(IEnumerable<long?> values)
    {
        long total = 0;
        var hasAny = false;
        foreach (var value in values)
        {
            if (value is null)
                continue;

            total += value.Value;
            hasAny = true;
        }

        return hasAny ? total : null;
    }

    private static double? AverageNullable(IEnumerable<double?> values)
    {
        var present = values.Where(value => value is not null).Select(value => value!.Value).ToList();
        return present.Count == 0 ? null : present.Average();
    }

    private long? GetDiskFreeBytes()
    {
        try
        {
            var path = _configuration["Operations:DataPath"] ?? "/opt/convy";
            var root = Path.GetPathRoot(path);
            if (string.IsNullOrWhiteSpace(root))
                return null;

            return new DriveInfo(root).AvailableFreeSpace;
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
            var path = _configuration["Operations:DataPath"] ?? "/opt/convy";
            var root = Path.GetPathRoot(path);
            if (string.IsNullOrWhiteSpace(root))
                return null;

            return new DriveInfo(root).TotalSize;
        }
        catch
        {
            return null;
        }
    }

    private static string? GetCpuModel()
    {
        try
        {
            const string cpuInfoPath = "/proc/cpuinfo";
            if (!File.Exists(cpuInfoPath))
                return null;

            return File.ReadLines(cpuInfoPath)
                .Select(line => line.Split(':', 2))
                .Where(parts => parts.Length == 2 && parts[0].Trim().Equals("model name", StringComparison.OrdinalIgnoreCase))
                .Select(parts => parts[1].Trim())
                .FirstOrDefault();
        }
        catch
        {
            return null;
        }
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

    private static DateTime? TryParseDateTime(string? value) =>
        DateTime.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal, out var parsed)
            ? parsed.ToUniversalTime()
            : null;
}
