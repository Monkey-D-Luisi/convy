using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Convy.Domain.Entities;
using Convy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Convy.API.Services;

public class McpWriteIdempotencyService
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();
    private readonly ConvyDbContext _context;

    public McpWriteIdempotencyService(ConvyDbContext context)
    {
        _context = context;
    }

    public async Task<McpIdempotencySnapshot> ExecuteAsync(
        HttpContext httpContext,
        string actionName,
        object requestFingerprint,
        Func<Task<McpIdempotencySnapshot>> execute,
        CancellationToken cancellationToken)
    {
        if (!IsMcp(httpContext))
            return await execute();

        var idempotencyKey = httpContext.Request.Headers["Idempotency-Key"].FirstOrDefault()
            ?? httpContext.Request.Headers["X-Idempotency-Key"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return McpIdempotencySnapshot.Json(StatusCodes.Status400BadRequest, null, new { error = "idempotency_key_required" });

        if (!Guid.TryParse(httpContext.User.FindFirst("sub")?.Value, out var userId))
            return McpIdempotencySnapshot.Json(StatusCodes.Status400BadRequest, null, new { error = "invalid_mcp_subject" });

        var clientId = httpContext.User.FindFirst("client_id")?.Value;
        if (string.IsNullOrWhiteSpace(clientId))
            return McpIdempotencySnapshot.Json(StatusCodes.Status400BadRequest, null, new { error = "invalid_mcp_client" });

        var now = DateTime.UtcNow;
        var keyHash = Hash(idempotencyKey);
        var requestHash = Hash(JsonSerializer.Serialize(new { actionName, requestFingerprint }, JsonOptions));
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = _context.Database.IsRelational()
                ? await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken)
                : null;
            var existing = await _context.McpIdempotencyRecords
                .AsNoTracking()
                .FirstOrDefaultAsync(record =>
                    record.UserId == userId
                    && record.ClientId == clientId
                    && record.KeyHash == keyHash
                    && record.ExpiresAt > now,
                    cancellationToken);

            if (existing is not null)
            {
                return string.Equals(existing.RequestHash, requestHash, StringComparison.Ordinal)
                    ? new McpIdempotencySnapshot(existing.StatusCode, existing.Location, existing.ResponseJson)
                    : McpIdempotencySnapshot.Json(StatusCodes.Status409Conflict, null, new { error = "idempotency_key_conflict" });
            }

            var snapshot = await execute();
            _context.McpIdempotencyRecords.Add(new McpIdempotencyRecord(
                userId,
                clientId,
                keyHash,
                actionName,
                requestHash,
                snapshot.StatusCode,
                snapshot.Location,
                snapshot.ResponseJson,
                now,
                now.AddHours(24)));
            await _context.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);

            return snapshot;
        });
    }

    private static bool IsMcp(HttpContext httpContext) =>
        string.Equals(httpContext.User.FindFirst("auth_source")?.Value, "mcp", StringComparison.Ordinal);

    private static string Hash(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Base64UrlEncoder.Encode(hash);
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}

public record McpIdempotencySnapshot(int StatusCode, string? Location, string? ResponseJson)
{
    public static McpIdempotencySnapshot Json(int statusCode, string? location, object body) =>
        new(statusCode, location, JsonSerializer.Serialize(body, McpWriteIdempotencySnapshotJson.Options));

    public static McpIdempotencySnapshot Empty(int statusCode) => new(statusCode, null, null);

    public static McpIdempotencySnapshot NoContent() => Empty(StatusCodes.Status204NoContent);

    public IResult ToResult()
    {
        if (StatusCode == StatusCodes.Status204NoContent)
            return Results.NoContent();

        if (ResponseJson is null)
            return Results.StatusCode(StatusCode);

        var body = JsonSerializer.Deserialize<JsonElement>(ResponseJson, McpWriteIdempotencySnapshotJson.Options);
        return StatusCode == StatusCodes.Status201Created && Location is not null
            ? Results.Created(Location, body)
            : Results.Json(body, statusCode: StatusCode);
    }
}

internal static class McpWriteIdempotencySnapshotJson
{
    public static readonly JsonSerializerOptions Options = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
