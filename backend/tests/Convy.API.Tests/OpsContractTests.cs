using FluentAssertions;

namespace Convy.API.Tests;

public class OpsContractTests
{
    [Fact]
    public void VpsCaddyfile_ShouldExposeApiAdminAuthMcpAndLegalHosts()
    {
        var source = ReadRepoFile("docker", "Caddyfile.vps");

        source.Should().Contain("{$CONVY_API_HOSTNAME}");
        source.Should().Contain("{$CONVY_ADMIN_HOSTNAME}");
        source.Should().Contain("{$CONVY_AUTH_HOSTNAME}");
        source.Should().Contain("{$CONVY_MCP_HOSTNAME}");
        source.Should().Contain("{$CONVY_LEGAL_HOSTNAME}");
        source.Should().Contain("{$CONVY_PUBLIC_HOSTNAME}");
        source.Should().Contain("{$CONVY_WWW_HOSTNAME}");
        source.Should().Contain("{$CONVY_LEGACY_API_HOSTNAME}");
        source.Should().Contain("{$CONVY_LEGACY_ADMIN_HOSTNAME}");
        source.Should().Contain("{$CONVY_LEGACY_AUTH_HOSTNAME}");
        source.Should().Contain("{$CONVY_LEGACY_MCP_HOSTNAME}");
        source.Should().Contain("{$CONVY_LEGACY_LEGAL_HOSTNAME}");
        source.Should().Contain("basic_auth");
        source.Should().Contain("reverse_proxy auth:3000");
        source.Should().Contain("reverse_proxy mcp:3001");
        source.Should().Contain("root * /srv/legal");
        source.Should().Contain("root * /srv/public");
    }

    [Fact]
    public void VpsCompose_ShouldRunDashboardAuthMcpAndMountLegalContent()
    {
        var source = ReadRepoFile("docker", "docker-compose.vps.yml");

        source.Should().Contain("dashboard:");
        source.Should().Contain("auth:");
        source.Should().Contain("mcp:");
        source.Should().Contain("../dashboard");
        source.Should().Contain("../auth");
        source.Should().Contain("../mcp");
        source.Should().Contain("/opt/convy/legal:/srv/legal:ro");
        source.Should().Contain("/opt/convy/public:/srv/public:ro");
        source.Should().Contain("/opt/convy/backups/postgres:/opt/convy/backups/postgres:ro");
        source.Should().Contain("CONVY_API_BASE_URL: http://api:8080");
        source.Should().Contain("MCP_JWT_PUBLIC_KEY_BASE64");
    }

    [Fact]
    public void VpsDeployScript_ShouldPublishLegalAndCheckReadyEndpoint()
    {
        var source = ReadRepoFile("ops", "vps", "deploy-release.sh");

        source.Should().Contain("/opt/convy/legal");
        source.Should().Contain("/opt/convy/public");
        source.Should().Contain("legal");
        source.Should().Contain("public-site");
        source.Should().Contain("CONVY_API_HOSTNAME");
        source.Should().Contain("CONVY_AUTH_HOSTNAME");
        source.Should().Contain("CONVY_MCP_HOSTNAME");
        source.Should().Contain("/health/ready");
        source.Should().Contain("/health");
    }

    [Fact]
    public void VpsEnvironmentExample_ShouldDefaultToConvyAppDotCom()
    {
        var source = ReadRepoFile("docker", ".env.vps.example");

        source.Should().Contain("CONVY_PUBLIC_HOSTNAME=convyapp.com");
        source.Should().Contain("CONVY_WWW_HOSTNAME=www.convyapp.com");
        source.Should().Contain("CONVY_API_HOSTNAME=api.convyapp.com");
        source.Should().Contain("CONVY_ADMIN_HOSTNAME=admin.convyapp.com");
        source.Should().Contain("CONVY_AUTH_HOSTNAME=auth.convyapp.com");
        source.Should().Contain("CONVY_MCP_HOSTNAME=mcp.convyapp.com");
        source.Should().Contain("CONVY_LEGAL_HOSTNAME=legal.convyapp.com");
        source.Should().Contain("CONVY_LEGACY_API_HOSTNAME=178.105.70.69.nip.io");
        source.Should().Contain("CONVY_LEGACY_ADMIN_HOSTNAME=admin.178.105.70.69.nip.io");
        source.Should().Contain("CONVY_LEGACY_AUTH_HOSTNAME=auth.178.105.70.69.nip.io");
        source.Should().Contain("CONVY_LEGACY_MCP_HOSTNAME=mcp.178.105.70.69.nip.io");
        source.Should().Contain("CONVY_LEGACY_LEGAL_HOSTNAME=legal.178.105.70.69.nip.io");
    }

    [Fact]
    public void AndroidStagingFlavor_ShouldDefaultToConvyAppApiHostAndKeepOverride()
    {
        var source = ReadRepoFile("mobile", "androidApp", "build.gradle.kts");

        source.Should().Contain("CONVY_STAGING_API_HOST");
        source.Should().Contain("\"api.convyapp.com\"");
        source.Should().NotContain("\"178.105.70.69.nip.io\")");
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
