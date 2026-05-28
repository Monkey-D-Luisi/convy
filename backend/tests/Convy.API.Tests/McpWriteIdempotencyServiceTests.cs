using Convy.API.Services;
using Convy.Domain.Entities;
using Convy.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace Convy.API.Tests;

public class McpWriteIdempotencyServiceTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsBadRequestForMcpWriteWithoutIdempotencyKey()
    {
        await using var context = CreateContext();
        var service = new McpWriteIdempotencyService(context);
        var httpContext = CreateMcpContext();
        var executed = false;

        var result = await service.ExecuteAsync(
            httpContext,
            "convy_create_shopping_item",
            new { listId = Guid.NewGuid(), title = "Milk" },
            () =>
            {
                executed = true;
                return Task.FromResult(McpIdempotencySnapshot.Json(201, "/items/1", new { id = "1" }));
            },
            CancellationToken.None);

        executed.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        result.ResponseJson.Should().Contain("idempotency_key_required");
    }

    [Fact]
    public async Task ExecuteAsync_ReplaysSameMcpWriteAndStoresOnlyHashes()
    {
        await using var context = CreateContext();
        var service = new McpWriteIdempotencyService(context);
        var httpContext = CreateMcpContext("stable-key");
        var executions = 0;

        var first = await service.ExecuteAsync(
            httpContext,
            "convy_create_task",
            new { listId = Guid.Parse("11111111-1111-4111-8111-111111111111"), title = "Clean kitchen" },
            () =>
            {
                executions += 1;
                return Task.FromResult(McpIdempotencySnapshot.Json(201, "/tasks/abc", new { id = "abc" }));
            },
            CancellationToken.None);
        var second = await service.ExecuteAsync(
            httpContext,
            "convy_create_task",
            new { listId = Guid.Parse("11111111-1111-4111-8111-111111111111"), title = "Clean kitchen" },
            () =>
            {
                executions += 1;
                return Task.FromResult(McpIdempotencySnapshot.Json(201, "/tasks/other", new { id = "other" }));
            },
            CancellationToken.None);

        executions.Should().Be(1);
        second.StatusCode.Should().Be(first.StatusCode);
        second.Location.Should().Be(first.Location);
        second.ResponseJson.Should().Be(first.ResponseJson);
        var record = await context.McpIdempotencyRecords.SingleAsync();
        record.KeyHash.Should().NotBe("stable-key");
        record.KeyHash.Should().NotBeNullOrWhiteSpace();
        record.RequestHash.Should().NotContain("Clean kitchen");
    }

    [Fact]
    public async Task ExecuteAsync_RejectsSameKeyWithDifferentRequest()
    {
        await using var context = CreateContext();
        var service = new McpWriteIdempotencyService(context);
        var httpContext = CreateMcpContext("stable-key");

        await service.ExecuteAsync(
            httpContext,
            "convy_create_shopping_item",
            new { listId = Guid.NewGuid(), title = "Milk" },
            () => Task.FromResult(McpIdempotencySnapshot.Json(201, "/items/abc", new { id = "abc" })),
            CancellationToken.None);

        var conflict = await service.ExecuteAsync(
            httpContext,
            "convy_create_shopping_item",
            new { listId = Guid.NewGuid(), title = "Bread" },
            () => Task.FromResult(McpIdempotencySnapshot.Json(201, "/items/other", new { id = "other" })),
            CancellationToken.None);

        conflict.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        conflict.ResponseJson.Should().Contain("idempotency_key_conflict");
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotRequireIdempotencyForFirebaseRequests()
    {
        await using var context = CreateContext();
        var service = new McpWriteIdempotencyService(context);
        var httpContext = new DefaultHttpContext();
        var executed = false;

        var result = await service.ExecuteAsync(
            httpContext,
            "convy_create_task",
            new { title = "Clean kitchen" },
            () =>
            {
                executed = true;
                return Task.FromResult(McpIdempotencySnapshot.NoContent());
            },
            CancellationToken.None);

        executed.Should().BeTrue();
        result.StatusCode.Should().Be(StatusCodes.Status204NoContent);
        context.McpIdempotencyRecords.Should().BeEmpty();
    }

    private static ConvyDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ConvyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ConvyDbContext(options);
    }

    private static DefaultHttpContext CreateMcpContext(string? idempotencyKey = null)
    {
        var context = new DefaultHttpContext();
        var identity = new ClaimsIdentity([
            new Claim("auth_source", "mcp"),
            new Claim("sub", "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa"),
            new Claim("client_id", "https://chatgpt.com/aip/g-123/.well-known/oauth-client"),
        ], "Bearer");
        context.User = new ClaimsPrincipal(identity);
        if (idempotencyKey is not null)
        {
            context.Request.Headers["Idempotency-Key"] = idempotencyKey;
        }

        return context;
    }
}
