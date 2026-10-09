using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Convy.Infrastructure.Services;

// The controller publishes only public fields through a read-only directory bind.
// Read on each request so static releases do not recreate the running backend.
internal sealed record AcceptedReleaseMetadata(string SourceSha, string AndroidVersion, DateTime AcceptedAtUtc)
{
    internal static async Task<AcceptedReleaseMetadata?> ReadAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            if (stream.Length > 4096) return null;
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = document.RootElement;
            var source = root.GetProperty("sourceSha").GetString();
            var backend = root.GetProperty("backendSourceSha").GetString();
            var android = root.GetProperty("androidVersion").GetString();
            var time = root.GetProperty("acceptedAtUtc").GetString();
            if (root.GetProperty("format").GetInt32() != 1 || backend is null || !Regex.IsMatch(backend, "\\A[a-f0-9]{40}\\z") ||
                source is null || !Regex.IsMatch(source, "\\A[a-f0-9]{40}\\z") ||
                android is null || !Regex.IsMatch(android, "\\A[A-Za-z0-9.+_-]{1,100}\\z") ||
                !DateTimeOffset.TryParse(time, CultureInfo.InvariantCulture, DateTimeStyles.None, out var at) ||
                at.Offset != TimeSpan.Zero) return null;
            return new(source, android, at.UtcDateTime);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or KeyNotFoundException or InvalidOperationException or FormatException)
        {
            return null; // Runtime configuration is the fallback for missing or corrupt public metadata.
        }
    }
}
