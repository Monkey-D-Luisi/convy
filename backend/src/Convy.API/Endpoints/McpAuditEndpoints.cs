using System.Security.Cryptography;
using System.Text;
using Convy.Domain.Entities;
using Convy.Domain.ValueObjects;
using Convy.Infrastructure.Persistence;

namespace Convy.API.Endpoints;

public static class McpAuditEndpoints
{
    private static readonly HashSet<string> AllowedTools =
    [
        "convy_get_context",
        "convy_get_shopping_context",
        "convy_get_shopping_list",
        "convy_get_task_list",
        "convy_get_recent_activity",
        "convy_add_shopping_items",
        "convy_update_shopping_items_status",
        "convy_add_tasks",
        "convy_update_tasks_status"
    ];

    public static void MapMcpAuditEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/mcp/audit")
            .WithTags("MCP Audit");

        group.MapPost("/tool-invocations", async (
            HttpContext httpContext,
            McpToolInvocationRequest request,
            ConvyDbContext db,
            IConfiguration configuration,
            CancellationToken cancellationToken) =>
        {
            var expectedKey = configuration["McpAudit:ApiKey"] ?? configuration["Mcp:AuditApiKey"];
            if (string.IsNullOrWhiteSpace(expectedKey))
                return Results.Problem("MCP audit API key is not configured.", statusCode: StatusCodes.Status500InternalServerError);

            var providedKey = httpContext.Request.Headers["X-Convy-Mcp-Audit-Key"].FirstOrDefault();
            if (!SecretEquals(expectedKey, providedKey))
                return Results.Unauthorized();

            if (request.UserId == Guid.Empty)
                return Results.BadRequest(new { error = "user_id is required." });
            if (request.HouseholdId == Guid.Empty)
                return Results.BadRequest(new { error = "household_id must not be empty." });
            if (!AllowedTools.Contains(request.ToolName))
                return Results.BadRequest(new { error = "tool_name is not supported." });
            if (!Enum.TryParse<McpToolInvocationStatus>(request.Status, ignoreCase: true, out var status))
                return Results.BadRequest(new { error = "status is not supported." });
            if (request.LatencyMs < 0)
                return Results.BadRequest(new { error = "latency_ms must not be negative." });

            var invocation = new McpToolInvocation(
                request.UserId,
                request.HouseholdId,
                request.ClientId,
                request.ToolName,
                status,
                request.LatencyMs,
                request.ErrorType);

            db.McpToolInvocations.Add(invocation);
            await db.SaveChangesAsync(cancellationToken);

            return Results.Accepted($"/api/v1/mcp/audit/tool-invocations/{invocation.Id}", new { id = invocation.Id });
        })
        .RequireRateLimiting("mcp-audit");
    }

    private static bool SecretEquals(string expected, string? actual)
    {
        if (string.IsNullOrEmpty(actual))
            return false;

        var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(expected));
        var actualHash = SHA256.HashData(Encoding.UTF8.GetBytes(actual));
        return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
    }
}

public record McpToolInvocationRequest(
    Guid UserId,
    Guid? HouseholdId,
    string? ClientId,
    string ToolName,
    string Status,
    long LatencyMs,
    string? ErrorType);
