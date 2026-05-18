using System.Data;
using System.Reflection;
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
        var itemsCreated7d = await _context.ListItems.CountAsync(i => i.CreatedAt >= since7d, cancellationToken);
        var itemsCompleted7d = await _context.ListItems.CountAsync(i => i.CompletedAt >= since7d, cancellationToken);
        var tasksCreated7d = await _context.TaskItems.CountAsync(t => t.CreatedAt >= since7d, cancellationToken);
        var tasksCompleted7d = await _context.TaskItems.CountAsync(t => t.CompletedAt >= since7d, cancellationToken);
        var voiceEvents7d = await _context.VoiceParseEvents
            .Where(v => v.CreatedAt >= since7d)
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
            voiceEvents7d.Count,
            voiceEvents7d.Count(v => v.Status == VoiceParseStatus.Success),
            voiceEvents7d.Count(v => v.Status != VoiceParseStatus.Success),
            voiceItemsCreated7d,
            SumNullable(voiceEvents7d.Select(v => v.EstimatedCostMicros)),
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
        var itemCreated = await _context.ListItems
            .Where(i => i.CreatedAt >= start && i.CreatedAt < end)
            .Select(i => i.CreatedAt)
            .ToListAsync(cancellationToken);
        var itemCompleted = await _context.ListItems
            .Where(i => i.CompletedAt >= start && i.CompletedAt < end)
            .Select(i => i.CompletedAt!.Value)
            .ToListAsync(cancellationToken);
        var taskCreated = await _context.TaskItems
            .Where(t => t.CreatedAt >= start && t.CreatedAt < end)
            .Select(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
        var taskCompleted = await _context.TaskItems
            .Where(t => t.CompletedAt >= start && t.CompletedAt < end)
            .Select(t => t.CompletedAt!.Value)
            .ToListAsync(cancellationToken);

        var metrics = days.Select(day => new DailyUsageMetricDto(
            day,
            activity.Where(a => DateOnly.FromDateTime(a.CreatedAt) == day).Select(a => a.HouseholdId).Distinct().Count(),
            itemCreated.Count(d => DateOnly.FromDateTime(d) == day),
            itemCompleted.Count(d => DateOnly.FromDateTime(d) == day),
            taskCreated.Count(d => DateOnly.FromDateTime(d) == day),
            taskCompleted.Count(d => DateOnly.FromDateTime(d) == day)))
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
            BackendVersion: Assembly.GetExecutingAssembly().GetName().Version?.ToString(),
            AndroidVersion: _configuration["Mobile:AndroidVersion"],
            LastDeployAt: TryParseDateTime(_configuration["Deploy:LastDeployAt"]));
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
