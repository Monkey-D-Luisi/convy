using FluentAssertions;

namespace Convy.API.Tests;

public class AdminEndpointContractTests
{
    [Fact]
    public void Program_ShouldExposeReadyHealthCheck()
    {
        var source = File.ReadAllText(Path.Combine(FindRepoRoot(), "backend", "src", "Convy.API", "Program.cs"));

        source.Should().Contain("MapHealthChecks(\"/health/ready\"");
        source.Should().Contain("AddDbContextCheck<ConvyDbContext>");
    }

    [Fact]
    public void Program_ShouldRegisterAdminOnlyPolicy()
    {
        var source = File.ReadAllText(Path.Combine(FindRepoRoot(), "backend", "src", "Convy.API", "Program.cs"));

        source.Should().Contain("AdminOnly");
        source.Should().Contain("AdminEmailRequirement");
    }

    [Fact]
    public void AdminEndpoints_ShouldExposeDashboardMetricsRoutes()
    {
        var source = File.ReadAllText(Path.Combine(FindRepoRoot(), "backend", "src", "Convy.API", "Endpoints", "AdminEndpoints.cs"));

        source.Should().Contain("/api/v1/admin");
        source.Should().Contain("/metrics/overview");
        source.Should().Contain("/metrics/usage");
        source.Should().Contain("/metrics/voice");
        source.Should().Contain("/metrics/openai");
        source.Should().Contain("/backups/latest");
        source.Should().Contain("/backups/runs");
        source.Should().Contain("/backups/runs/{id:guid}/download");
        source.Should().Contain("/system/health");
        source.Should().Contain("RequireAuthorization(\"AdminOnly\")");
    }

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
