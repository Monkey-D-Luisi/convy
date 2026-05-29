using FluentAssertions;
using System.Text.Json;

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
        source.Should().Contain("/system/history");
        source.Should().Contain("/mcp/overview");
        source.Should().Contain("RequireAuthorization(\"AdminOnly\")");
    }

    [Fact]
    public void AdminEndpoints_ShouldSerializeLatestBackupNullAsJson()
    {
        var source = File.ReadAllText(Path.Combine(FindRepoRoot(), "backend", "src", "Convy.API", "Endpoints", "AdminEndpoints.cs"));

        source.Should().Contain("Results.Json(result.Value)");
    }

    [Fact]
    public void AdminMcpOverviewDto_ShouldSerializeStableJsonContract()
    {
        var dtoSource = File.ReadAllText(Path.Combine(FindRepoRoot(), "backend", "src", "Convy.Application", "Features", "Admin", "DTOs", "AdminDtos.cs"));

        dtoSource.Should().Contain("JsonPropertyName(\"oauth\")");
        dtoSource.Should().NotContain("JsonPropertyName(\"oAuth\")");
        dtoSource.Should().Contain("AdminMcpRuntimeDto Runtime");
        dtoSource.Should().Contain("AdminMcpOAuthMetricsDto OAuth");
        dtoSource.Should().Contain("AdminMcpUsageMetricsDto Usage");
        dtoSource.Should().Contain("IReadOnlyList<McpToolCatalogItemDto> ToolCatalog");
        dtoSource.Should().Contain("IReadOnlyList<McpPublicationReadinessCheckDto> ReadinessChecks");
    }

    [Fact]
    public void AdminSystemHistoryDto_ShouldSerializeStableJsonContract()
    {
        var dtoSource = File.ReadAllText(Path.Combine(FindRepoRoot(), "backend", "src", "Convy.Application", "Features", "Admin", "DTOs", "AdminDtos.cs"));
        var querySource = File.ReadAllText(Path.Combine(FindRepoRoot(), "backend", "src", "Convy.Application", "Features", "Admin", "Queries", "AdminQueries.cs"));
        var handlerSource = File.ReadAllText(Path.Combine(FindRepoRoot(), "backend", "src", "Convy.Application", "Features", "Admin", "Queries", "AdminQueryHandlers.cs"));

        dtoSource.Should().Contain("public record AdminSystemHistoryDto");
        dtoSource.Should().Contain("public record SystemMetricSnapshotDto");
        dtoSource.Should().Contain("DateTime CapturedAt");
        dtoSource.Should().Contain("long? DiskFreeBytes");
        dtoSource.Should().Contain("long? DiskTotalBytes");
        dtoSource.Should().Contain("long? MemoryAvailableBytes");
        dtoSource.Should().Contain("long? MemoryTotalBytes");
        dtoSource.Should().Contain("double? LoadAverage1m");
        dtoSource.Should().Contain("long? UptimeSeconds");
        dtoSource.Should().Contain("long? PostgresDataSizeBytes");
        querySource.Should().Contain("GetAdminSystemHistoryQuery");
        handlerSource.Should().Contain("GetAdminSystemHistoryQueryHandler");
        handlerSource.Should().Contain("AdminDateRangePolicy.Validate");
    }

    [Fact]
    public void CurrentUserService_ShouldNotUseSyncOverAsyncFallback()
    {
        var source = File.ReadAllText(Path.Combine(FindRepoRoot(), "backend", "src", "Convy.API", "Services", "CurrentUserService.cs"));

        source.Should().NotContain(".GetAwaiter().GetResult()");
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
