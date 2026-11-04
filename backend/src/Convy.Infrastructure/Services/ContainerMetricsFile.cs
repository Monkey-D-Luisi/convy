using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Convy.Application.Features.Admin.DTOs;

namespace Convy.Infrastructure.Services;

internal static class ContainerMetricsFile
{
    public const string DefaultPath = "/run/convy-container-metrics/current.json";
    private static readonly string[] Names = ["convy-api", "convy-worker", "convy-auth", "convy-dashboard", "convy-mcp", "convy-db"];
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    public static async Task<ContainerMetricsDto> ReadAsync(string path, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            if (stream.Length > 16384) return Unknown();
            var bytes = new byte[16385];
            var count = 0;
            while (count < bytes.Length)
            {
                var read = await stream.ReadAsync(bytes.AsMemory(count), cancellationToken);
                if (read == 0) break;
                count += read;
            }
            if (count > 16384) return Unknown();
            return Parse(System.Text.Encoding.UTF8.GetString(bytes, 0, count), now);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or ArgumentException or NotSupportedException)
        {
            return Unknown();
        }
    }

    public static ContainerMetricsDto Parse(string? json, DateTimeOffset? now = null)
    {
        try
        {
            var sample = json is null ? null : JsonSerializer.Deserialize<ContainerMetricsDto>(json, Json);
            if (sample is null || sample.SchemaVersion != 1 || sample.Scope != "container" || sample.Product != "convy" ||
                sample.CapturedAtUtc == default || sample.CapturedAtUtc.Offset != TimeSpan.Zero ||
                sample.Status is not ("available" or "unknown" or "stale") ||
                sample.Services is null || sample.Services.Count != Names.Length || sample.Services.Any(s => s is null) ||
                !sample.Services.Select(s => s.ContainerName).Order().SequenceEqual(Names.Order()) ||
                sample.Services.Any(s => !Valid(s, sample.CapturedAtUtc)) ||
                sample.Services.Where(s => s.ContainerId is not null).Select(s => s.ContainerId).Distinct().Count() != sample.Services.Count(s => s.ContainerId is not null))
                return Unknown();
            if (now is { } observed)
            {
                if (sample.CapturedAtUtc > observed.AddSeconds(30)) return Unknown();
                if (observed - sample.CapturedAtUtc > TimeSpan.FromSeconds(180)) return Unknown() with { Status = "stale", CapturedAtUtc = sample.CapturedAtUtc };
            }
            return sample;
        }
        catch (JsonException) { return Unknown(); }
    }

    public static string Serialize(ContainerMetricsDto sample) => JsonSerializer.Serialize(sample, Json);

    public static ContainerMetricsDto Unknown() => new(1, "container", "convy", default,
        Names.Select(name => new ContainerServiceMetricDto(name, null, "unknown", "unknown", null, null, null, null)).ToArray(), "unknown");

    private static bool Valid(ContainerServiceMetricDto service, DateTimeOffset captured) =>
        (service.ContainerId is null || Regex.IsMatch(service.ContainerId, "\\A[0-9a-f]{64}\\z")) &&
        service.State is "running" or "exited" or "restarting" or "created" or "paused" or "dead" or "removing" or "missing" or "unknown" &&
        service.Health is "healthy" or "unhealthy" or "starting" or "unknown" &&
        (service.CpuPercent is null || double.IsFinite(service.CpuPercent.Value) && service.CpuPercent >= 0) &&
        service.MemoryBytes is not < 0 && service.RestartCount is not < 0 &&
        (service.StartedAtUtc is null || service.StartedAtUtc.Value.Offset == TimeSpan.Zero && service.StartedAtUtc <= captured) &&
        (service.ContainerId is not null || service.CpuPercent is null && service.MemoryBytes is null && service.RestartCount is null && service.StartedAtUtc is null) &&
        (service.State == "running" || service.CpuPercent is null && service.MemoryBytes is null);
}
