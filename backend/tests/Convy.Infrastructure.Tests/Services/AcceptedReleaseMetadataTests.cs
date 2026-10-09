using System.Text.Json;
using Convy.Infrastructure.Services;
using FluentAssertions;

namespace Convy.Infrastructure.Tests.Services;

public class AcceptedReleaseMetadataTests
{
    [Fact]
    public async Task StaticAcceptanceAndRollbackAreVisibleWithoutRestartingBackend()
    {
        var path = Path.GetTempFileName();
        var backend = new string('b', 40);
        try
        {
            foreach (var release in new[] { new string('a', 40), new string('c', 40), new string('a', 40) })
            {
                await File.WriteAllTextAsync(path, JsonSerializer.Serialize(new { format = 1, sourceSha = release,
                    backendSourceSha = backend, androidVersion = "1.2.3+42", acceptedAtUtc = "2026-10-09T12:00:00+00:00" }));
                var accepted = await AcceptedReleaseMetadata.ReadAsync(path, default);
                accepted!.SourceSha.Should().Be(release);
                accepted.AndroidVersion.Should().Be("1.2.3+42");
                accepted.AcceptedAtUtc.Kind.Should().Be(DateTimeKind.Utc);
            }
        }
        finally { File.Delete(path); }
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData("{\"format\":2}")]
    public async Task InvalidMetadataFallsBackToRuntimeConfiguration(string content)
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, content);
            (await AcceptedReleaseMetadata.ReadAsync(path, default)).Should().BeNull();
            await File.WriteAllTextAsync(path, new string('x', 4097));
            (await AcceptedReleaseMetadata.ReadAsync(path, default)).Should().BeNull();
        }
        finally { File.Delete(path); }
        (await AcceptedReleaseMetadata.ReadAsync(path, default)).Should().BeNull();
    }
}
