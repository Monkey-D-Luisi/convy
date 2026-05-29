using Convy.Domain.Entities;
using Convy.Domain.ValueObjects;
using FluentAssertions;

namespace Convy.Domain.Tests.Entities;

public class McpOAuthEntityTests
{
    [Fact]
    public void AuthorizationCode_WithValidData_CreatesReusableSafeRecord()
    {
        var userId = Guid.NewGuid();
        var expiresAt = DateTime.UtcNow.AddMinutes(5);

        var code = new McpOAuthAuthorizationCode(
            "code-hash",
            userId,
            "https://chat.openai.com/mcp/client.json",
            "https://chat.openai.com/callback",
            "https://mcp.convyapp.com",
            "convy.households.read convy.lists.read",
            "pkce-challenge",
            "S256",
            expiresAt);

        code.CodeHash.Should().Be("code-hash");
        code.UserId.Should().Be(userId);
        code.Scopes.Should().Be("convy.households.read convy.lists.read");
        code.ExpiresAt.Should().Be(expiresAt);
        code.UsedAt.Should().BeNull();

        code.MarkUsed(DateTime.UtcNow);

        code.UsedAt.Should().NotBeNull();
    }

    [Fact]
    public void AuthorizationCode_WithPastExpiry_ThrowsArgumentException()
    {
        var act = () => new McpOAuthAuthorizationCode(
            "code-hash",
            Guid.NewGuid(),
            "https://chat.openai.com/mcp/client.json",
            "https://chat.openai.com/callback",
            "https://mcp.convyapp.com",
            "convy.households.read",
            "pkce-challenge",
            "S256",
            DateTime.UtcNow.AddMinutes(-1));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RefreshToken_RevokeAndRotate_TracksOnlyTokenHashes()
    {
        var token = new McpOAuthRefreshToken(
            "refresh-hash",
            Guid.NewGuid(),
            "https://chat.openai.com/mcp/client.json",
            "https://mcp.convyapp.com",
            "convy.households.read",
            DateTime.UtcNow.AddDays(30));

        token.TokenHash.Should().Be("refresh-hash");
        token.RevokedAt.Should().BeNull();
        token.ReplacedByTokenHash.Should().BeNull();

        token.RotateTo("replacement-hash", DateTime.UtcNow);

        token.RevokedAt.Should().NotBeNull();
        token.ReplacedByTokenHash.Should().Be("replacement-hash");
    }

    [Fact]
    public void ToolInvocation_WithValidData_DoesNotStorePromptOrArguments()
    {
        var invocation = new McpToolInvocation(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "convy_get_context",
            McpToolInvocationStatus.Success,
            42,
            null);

        invocation.ToolName.Should().Be("convy_get_context");
        invocation.Status.Should().Be(McpToolInvocationStatus.Success);
        invocation.LatencyMs.Should().Be(42);

        typeof(McpToolInvocation).GetProperties()
            .Select(property => property.Name)
            .Should()
            .NotContain(name => name.Contains("Prompt", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Argument", StringComparison.OrdinalIgnoreCase));
    }
}
