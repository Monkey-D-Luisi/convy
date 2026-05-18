using FluentAssertions;

namespace Convy.API.Tests;

public class OpsContractTests
{
    [Fact]
    public void VpsCaddyfile_ShouldExposeApiAdminAndLegalHosts()
    {
        var source = ReadRepoFile("docker", "Caddyfile.vps");

        source.Should().Contain("{$CONVY_API_HOSTNAME}");
        source.Should().Contain("{$CONVY_ADMIN_HOSTNAME}");
        source.Should().Contain("{$CONVY_LEGAL_HOSTNAME}");
        source.Should().Contain("basicauth");
        source.Should().Contain("root * /srv/legal");
    }

    [Fact]
    public void VpsCompose_ShouldRunDashboardAndMountLegalContent()
    {
        var source = ReadRepoFile("docker", "docker-compose.vps.yml");

        source.Should().Contain("dashboard:");
        source.Should().Contain("../dashboard");
        source.Should().Contain("/opt/convy/legal:/srv/legal:ro");
        source.Should().Contain("CONVY_API_BASE_URL: http://api:8080");
    }

    [Fact]
    public void VpsDeployScript_ShouldPublishLegalAndCheckReadyEndpoint()
    {
        var source = ReadRepoFile("ops", "vps", "deploy-release.sh");

        source.Should().Contain("/opt/convy/legal");
        source.Should().Contain("legal");
        source.Should().Contain("CONVY_API_HOSTNAME");
        source.Should().Contain("/health/ready");
    }

    [Fact]
    public void VpsBackupScripts_ShouldUsePgDumpChecksumMetadataAndRestoreVerification()
    {
        var backup = ReadRepoFile("ops", "vps", "backups", "backup-postgres.sh");
        var restore = ReadRepoFile("ops", "vps", "backups", "restore-postgres.sh");
        var verify = ReadRepoFile("ops", "vps", "backups", "verify-backup.sh");

        backup.Should().Contain("flock");
        backup.Should().Contain("pg_dump");
        backup.Should().Contain("sha256sum");
        backup.Should().Contain("backup_runs");
        verify.Should().Contain("pg_restore --list");
        restore.Should().Contain("pg_restore");
    }

    [Fact]
    public void LegalDocuments_ShouldBeSelfContained()
    {
        var privacy = ReadRepoFile("legal", "privacy-policy.html");
        var terms = ReadRepoFile("legal", "terms.html");

        privacy.Should().NotContain("fonts.googleapis.com");
        privacy.Should().Contain("Hetzner");
        privacy.Should().Contain("OpenAI");
        terms.Should().Contain("Terms");
    }

    private static string ReadRepoFile(params string[] segments) =>
        File.ReadAllText(Path.Combine([FindRepoRoot(), .. segments]));

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "backend", "src", "Convy.API", "Program.cs")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root could not be found.");
    }
}
