using Microsoft.AspNetCore.Authorization;

namespace Convy.API.Authorization;

public class McpScopeRequirement : IAuthorizationRequirement
{
    public McpScopeRequirement(string scope)
    {
        Scope = scope;
    }

    public string Scope { get; }
}

public class McpScopeAuthorizationHandler : AuthorizationHandler<McpScopeRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        McpScopeRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
            return Task.CompletedTask;

        var authSource = context.User.FindFirst("auth_source")?.Value;
        if (!string.Equals(authSource, "mcp", StringComparison.Ordinal))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var scopes = context.User.FindAll("scope")
            .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .ToHashSet(StringComparer.Ordinal);

        if (scopes.Contains(requirement.Scope))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
