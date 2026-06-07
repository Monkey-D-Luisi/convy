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
        var backendRelease = ReadRepoFile(".github", "workflows", "backend-staging-release.yml");

        source.Should().Contain("/opt/convy/legal");
        source.Should().Contain("/opt/convy/public");
        source.Should().Contain("legal");
        source.Should().Contain("public-site");
        source.Should().Contain("CONVY_API_HOSTNAME");
        source.Should().Contain("CONVY_AUTH_HOSTNAME");
        source.Should().Contain("CONVY_MCP_HOSTNAME");
        source.Should().Contain("/health/ready");
        source.Should().Contain("/health");
        backendRelease.Should().Contain("API_HOSTNAME");
        backendRelease.Should().Contain("/health/ready");
        backendRelease.Should().NotContain("PUBLIC_HOSTNAME}/health");
        backendRelease.Should().Contain("Ensure non-root deploy user");
        backendRelease.Should().Contain("BOOTSTRAP_DEPLOY_USER");
        backendRelease.Should().Contain("vars.STAGING_DEPLOY_USER || 'convy-deploy'");
    }

    [Fact]
    public void AndroidPlayInternalWorkflow_ShouldBeProtectedAndAvoidPublicArtifacts()
    {
        var workflow = ReadRepoFile(".github", "workflows", "android-play-internal.yml");

        workflow.Should().Contain("name: Android Play Internal Release");
        workflow.Should().Contain("workflow_run");
        workflow.Should().Contain("workflows: [\"Continuous Integration\"]");
        workflow.Should().Contain("branches: [master]");
        workflow.Should().Contain("workflow_dispatch");
        workflow.Should().Contain("environment: android-release");
        workflow.Should().Contain("permissions:");
        workflow.Should().Contain("contents: read");
        workflow.Should().Contain("ANDROID_GOOGLE_SERVICES_JSON_B64");
        workflow.Should().Contain("ANDROID_KEYSTORE_PROPERTIES_B64");
        workflow.Should().Contain("ANDROID_RELEASE_KEYSTORE_B64");
        workflow.Should().Contain("GOOGLE_PLAY_SERVICE_ACCOUNT_JSON_B64");
        workflow.Should().Contain("bundleStagingRelease");
        workflow.Should().Contain("GOOGLE_PLAY_TRACK");
        workflow.Should().Contain("internal");
        workflow.Should().Contain("Remove restored secret files");
        workflow.Should().NotContain("actions/upload-artifact");
        workflow.Should().Contain("should_publish");
        workflow.Should().Contain("mobile/androidApp/build.gradle.kts");
        workflow.Should().Contain("needs: preflight");
        workflow.Should().Contain("needs.preflight.outputs.should_publish == 'true'");
    }

    [Fact]
    public void VpsDeployScript_ShouldPruneDockerBuildCacheAfterHealthyDeploy()
    {
        var source = ReadRepoFile("ops", "vps", "deploy-release.sh");

        source.Should().Contain("DOCKER_BUILD_CACHE_MAX_USED_SPACE");
        source.Should().Contain("docker buildx prune");
        source.Should().Contain("--max-used-space");
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
    public void AndroidLocalRelease_ShouldBeDisabledAndSensitiveTokensShouldNotUseUrlsOrLogs()
    {
        var gradle = ReadRepoFile("mobile", "androidApp", "build.gradle.kts");
        var messagingService = ReadRepoFile("mobile", "androidApp", "src", "main", "kotlin", "com", "convy", "ConvyFirebaseMessagingService.kt");
        var signalR = ReadRepoFile("mobile", "shared", "src", "commonMain", "kotlin", "com", "convy", "shared", "data", "remote", "SignalRClient.kt");
        var api = ReadRepoFile("mobile", "shared", "src", "commonMain", "kotlin", "com", "convy", "shared", "data", "remote", "ConvyApi.kt");

        gradle.Should().Contain("androidComponents");
        gradle.Should().Contain("variantBuilder.enable = false");
        messagingService.Should().NotContain("New token:");
        messagingService.Should().NotContain("Log.d(\"FCM\", \"New token");
        signalR.Should().NotContain("parameter(\"access_token\", token)");
        signalR.Should().Contain("header(\"Authorization\", \"Bearer $token\")");
        api.Should().NotContain("client.delete(\"api/v1/devices/$token\")");
        api.Should().Contain("UnregisterDeviceRequest(token)");
    }

    [Fact]
    public void DeploymentSecretsAndBootstrapScripts_ShouldUseSafeDefaults()
    {
        var vpsSecrets = ReadRepoFile("ops", "vps", "push-secrets.ps1");
        var ociSecrets = ReadRepoFile("ops", "oci", "push-secrets.ps1");
        var vpsBootstrap = ReadRepoFile("ops", "vps", "bootstrap-server.sh");
        var ociBootstrap = ReadRepoFile("ops", "oci", "bootstrap-server.sh");
        var hetznerVariables = ReadRepoFile("infra", "hetzner", "variables.tf");
        var ociVariables = ReadRepoFile("infra", "oci", "variables.tf");

        vpsSecrets.Should().Contain("install -m 600 -o root -g root /tmp/convy-api.env /opt/convy/shared/api.env");
        vpsSecrets.Should().Contain("install -m 640 -o root -g 1654 /tmp/convy-firebase-admin.json /opt/convy/shared/firebase-admin.json");
        vpsSecrets.Should().Contain("chmod 640 /opt/convy/shared/firebase-admin.json");
        ociSecrets.Should().Contain("chmod 600 /opt/convy/shared/firebase-admin.json");
        vpsBootstrap.Should().Contain("ALLOW_FORMAT_DATA_DEVICE");
        ociBootstrap.Should().Contain("ALLOW_FORMAT_DATA_DEVICE");
        hetznerVariables.Should().NotContain("default     = [\"0.0.0.0/0\"]");
        ociVariables.Should().NotContain("default     = [\"0.0.0.0/0\"]");
    }

    [Fact]
    public void DiagnosticsAndCi_ShouldNotCommitSensitiveRuntimeLogsAndShouldValidateOps()
    {
        var root = FindRepoRoot();
        var gitignore = ReadRepoFile(".gitignore");
        var ci = ReadRepoFile(".github", "workflows", "ci.yml");

        File.Exists(Path.Combine(root, "backend", "diag-logs.json")).Should().BeFalse();
        gitignore.Should().Contain("backend/diag-logs*.json");
        ci.Should().Contain("Validate compose files");
        ci.Should().Contain("Validate deploy scripts");
        ci.Should().Contain("docker compose -f docker/docker-compose.vps.yml config --quiet");
    }

    [Fact]
    public void NextApps_ShouldDefineSecurityHeaders()
    {
        var dashboardConfig = ReadRepoFile("dashboard", "next.config.ts");
        var authConfig = ReadRepoFile("auth", "next.config.ts");

        dashboardConfig.Should().Contain("Content-Security-Policy");
        dashboardConfig.Should().Contain("frame-ancestors 'none'");
        dashboardConfig.Should().Contain("X-Frame-Options");
        authConfig.Should().Contain("Content-Security-Policy");
        authConfig.Should().Contain("frame-ancestors 'none'");
        authConfig.Should().Contain("X-Frame-Options");
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
