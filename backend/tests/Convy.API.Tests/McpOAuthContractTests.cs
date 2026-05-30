using FluentAssertions;

namespace Convy.API.Tests;

public class McpOAuthContractTests
{
    [Fact]
    public void Program_ShouldRegisterFirebaseAndMcpBearerSchemesBehindSelector()
    {
        var source = ReadApiFile("Program.cs");

        source.Should().Contain("FirebaseBearer");
        source.Should().Contain("McpBearer");
        source.Should().Contain("AddPolicyScheme");
        source.Should().Contain("token_use");
        source.Should().Contain("mcp_access");
        source.Should().NotContain("missing-mcp-public-key-placeholder");
    }

    [Fact]
    public void McpBearer_ShouldPreserveMcpClaimsAndTagAuthSource()
    {
        var source = ReadApiFile("Program.cs");

        source.Should().Contain("MapInboundClaims = false");
        source.Should().Contain("OnTokenValidated");
        source.Should().Contain("auth_source");
        source.Should().Contain("mcp");
    }

    [Fact]
    public void Program_ShouldExposeOAuthMetadataTokenAndRevokeEndpoints()
    {
        var source = ReadEndpointFile("McpOAuthEndpoints.cs");

        source.Should().Contain("/.well-known/oauth-protected-resource");
        source.Should().Contain("/.well-known/oauth-authorization-server");
        source.Should().Contain("/.well-known/openid-configuration");
        source.Should().Contain("/oauth/token");
        source.Should().Contain("/oauth/revoke");
        source.Should().Contain("/api/v1/mcp/oauth/authorize/approve");
        source.Should().Contain("client_id_metadata_document_supported");
        source.Should().Contain("McpScopes.Supported");
        var scopes = ReadApiFile(Path.Combine("Authorization", "McpScopes.cs"));
        scopes.Should().Contain("convy.items.write");
        scopes.Should().Contain("convy.tasks.write");
    }

    [Fact]
    public void Program_ShouldExposeNarrowMcpAuditEndpointWithoutArgumentStorage()
    {
        var program = ReadApiFile("Program.cs");
        var source = ReadEndpointFile("McpAuditEndpoints.cs");

        program.Should().Contain("MapMcpAuditEndpoints");
        source.Should().Contain("/api/v1/mcp/audit");
        source.Should().Contain("/tool-invocations");
        source.Should().Contain("X-Convy-Mcp-Audit-Key");
        source.Should().Contain("AllowedTools");
        source.Should().NotContain("Prompt");
        source.Should().NotContain("Arguments");
    }

    [Fact]
    public void ReadEndpoints_ShouldRequireMcpReadScopePolicies()
    {
        ReadEndpointFile("HouseholdEndpoints.cs").Should().Contain("McpScopes.HouseholdsRead");
        ReadEndpointFile("ListEndpoints.cs").Should().Contain("McpScopes.ListsRead");
        ReadEndpointFile("ItemEndpoints.cs").Should().Contain("McpScopes.ItemsRead");
        ReadEndpointFile("TaskEndpoints.cs").Should().Contain("McpScopes.TasksRead");
        ReadEndpointFile("ActivityEndpoints.cs").Should().Contain("McpScopes.ActivityRead");
    }

    [Fact]
    public void WriteEndpoints_ShouldExposeOnlyLimitedMcpWriteScopePolicies()
    {
        ReadEndpointFile("HouseholdEndpoints.cs").Should().Contain("FirebaseOnly");
        ReadEndpointFile("ListEndpoints.cs").Should().Contain("FirebaseOnly");
        ReadEndpointFile("ItemEndpoints.cs").Should().Contain("McpPolicyNames.OnlyScope(McpScopes.ItemsWrite)");
        ReadEndpointFile("TaskEndpoints.cs").Should().Contain("McpPolicyNames.OnlyScope(McpScopes.TasksWrite)");
        ReadEndpointFile("ItemEndpoints.cs").Should().Contain("McpWriteIdempotencyService");
        ReadEndpointFile("TaskEndpoints.cs").Should().Contain("McpWriteIdempotencyService");
        ReadEndpointFile("ItemEndpoints.cs").Should().Contain("smart-batch");
        ReadEndpointFile("ItemEndpoints.cs").Should().Contain("status-batch");
        ReadEndpointFile("TaskEndpoints.cs").Should().Contain("smart-batch");
        ReadEndpointFile("TaskEndpoints.cs").Should().Contain("status-batch");
        ReadEndpointFile("UserEndpoints.cs").Should().Contain("FirebaseOnly");
    }

    [Fact]
    public void Program_ShouldRegisterMcpOnlyScopePoliciesForSmartWriteEndpoints()
    {
        var source = ReadApiFile("Program.cs");

        source.Should().Contain("McpPolicyNames.OnlyScope(scope)");
        source.Should().Contain("policy.AddAuthenticationSchemes(AuthSchemes.McpBearer)");
    }

    [Fact]
    public void DestructiveAndAdministrativeEndpoints_ShouldRemainFirebaseOnly()
    {
        var items = ReadEndpointFile("ItemEndpoints.cs");
        var tasks = ReadEndpointFile("TaskEndpoints.cs");
        var lists = ReadEndpointFile("ListEndpoints.cs");

        items.Should().Contain("MapPut(\"/{itemId:guid}\"");
        items.Should().Contain("MapDelete(\"/{itemId:guid}\"");
        items.Should().Contain(".RequireAuthorization(\"FirebaseOnly\")");
        tasks.Should().Contain("MapPut(\"/{taskId:guid}\"");
        tasks.Should().Contain("MapDelete(\"/{taskId:guid}\"");
        tasks.Should().Contain(".RequireAuthorization(\"FirebaseOnly\")");
        lists.Should().Contain("archive");
        lists.Should().Contain(".RequireAuthorization(\"FirebaseOnly\")");
    }

    [Fact]
    public void McpIdempotencyPersistence_ShouldHashKeysAndAvoidSensitiveStorage()
    {
        var contextSource = File.ReadAllText(Path.Combine(FindRepoRoot(), "backend", "src", "Convy.Infrastructure", "Persistence", "ConvyDbContext.cs"));
        var entitySource = File.ReadAllText(Path.Combine(FindRepoRoot(), "backend", "src", "Convy.Domain", "Entities", "McpIdempotencyRecord.cs"));
        var configurationSource = File.ReadAllText(Path.Combine(FindRepoRoot(), "backend", "src", "Convy.Infrastructure", "Persistence", "Configurations", "McpIdempotencyRecordConfiguration.cs"));

        contextSource.Should().Contain("DbSet<McpIdempotencyRecord>");
        entitySource.Should().Contain("KeyHash");
        entitySource.Should().Contain("RequestHash");
        entitySource.Should().NotContain("RawKey");
        entitySource.Should().NotContain("Prompt");
        entitySource.Should().NotContain("Arguments");
        configurationSource.Should().Contain("mcp_idempotency_records");
        configurationSource.Should().Contain("ix_mcp_idempotency_records_user_client_key");
        configurationSource.Should().Contain("IsUnique()");
    }

    [Fact]
    public void McpOAuthService_ShouldUseSerializableTransactionsForTokenRedemption()
    {
        var source = ReadApiFile(Path.Combine("Services", "McpOAuthService.cs"));
        var infrastructure = File.ReadAllText(Path.Combine(FindRepoRoot(), "backend", "src", "Convy.Infrastructure", "DependencyInjection.cs"));

        source.Should().Contain("BeginTransactionAsync(System.Data.IsolationLevel.Serializable");
        source.Should().Contain("RedeemAuthorizationCodeAsync");
        source.Should().Contain("RedeemRefreshTokenAsync");
        source.Should().Contain("CommitAsync(cancellationToken)");
        source.Should().Contain("CreateExecutionStrategy()");
        source.Should().Contain("ExecuteAsync(async () =>");
        infrastructure.Should().Contain("EnableRetryOnFailure");
        infrastructure.Should().Contain("\"40001\"");
        infrastructure.Should().Contain("\"40P01\"");
    }

    [Fact]
    public void McpWriteIdempotencyService_ShouldReserveAndExecuteInsideSerializableTransaction()
    {
        var source = ReadApiFile(Path.Combine("Services", "McpWriteIdempotencyService.cs"));

        source.Should().Contain("BeginTransactionAsync(System.Data.IsolationLevel.Serializable");
        source.Should().Contain("await transaction.CommitAsync(cancellationToken)");
        source.Should().Contain("CreateExecutionStrategy()");
        source.Should().Contain("ExecuteAsync(async () =>");
        source.Should().Contain("idempotency_key_expired");
    }

    [Fact]
    public void AdminAuthorization_ShouldRejectMcpTokensExplicitly()
    {
        var source = ReadApiFile(Path.Combine("Authorization", "AdminEmailRequirement.cs"));

        source.Should().Contain("auth_source");
        source.Should().Contain("mcp");
    }

    private static string ReadApiFile(string relativePath) =>
        File.ReadAllText(Path.Combine(FindRepoRoot(), "backend", "src", "Convy.API", relativePath));

    private static string ReadEndpointFile(string fileName) =>
        ReadApiFile(Path.Combine("Endpoints", fileName));

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
