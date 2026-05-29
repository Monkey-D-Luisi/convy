using System.Reflection;
using System.Net;
using Convy.Domain.Common;
using Convy.Domain.Entities;
using Convy.Domain.ValueObjects;
using Convy.Infrastructure.Persistence;
using Convy.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Convy.Infrastructure.Tests.Services;

public class AdminMetricsReaderTests
{
    [Fact]
    public async Task GetUsageAsync_CountsHistoricalItemEventsFromActivityLogs()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var household = new Household("Home", userId);
        var list = new HouseholdList("Shopping", ListType.Shopping, household.Id, userId);
        var deletedItemId = Guid.NewGuid();
        var reopenedItemId = Guid.NewGuid();
        var currentlyCompletedItem = new ListItem("Milk", list.Id, userId);
        SetId(currentlyCompletedItem, reopenedItemId);
        SetDate(currentlyCompletedItem, nameof(ListItem.CreatedAt), new DateTime(2026, 5, 7, 10, 0, 0, DateTimeKind.Utc));
        SetDate(currentlyCompletedItem, nameof(ListItem.CompletedAt), new DateTime(2026, 5, 13, 16, 0, 0, DateTimeKind.Utc));
        typeof(ListItem).GetProperty(nameof(ListItem.IsCompleted))!.SetValue(currentlyCompletedItem, true);

        context.Households.Add(household);
        context.HouseholdLists.Add(list);
        context.ListItems.Add(currentlyCompletedItem);
        context.ActivityLogs.AddRange(
            CreateLog(household.Id, deletedItemId, ActivityActionType.Created, userId, new DateTime(2026, 5, 8, 8, 0, 0, DateTimeKind.Utc)),
            CreateLog(household.Id, deletedItemId, ActivityActionType.Completed, userId, new DateTime(2026, 5, 8, 8, 5, 0, DateTimeKind.Utc)),
            CreateLog(household.Id, deletedItemId, ActivityActionType.Deleted, userId, new DateTime(2026, 5, 8, 8, 10, 0, DateTimeKind.Utc)),
            CreateLog(household.Id, reopenedItemId, ActivityActionType.Created, userId, new DateTime(2026, 5, 7, 10, 0, 0, DateTimeKind.Utc)),
            CreateLog(household.Id, reopenedItemId, ActivityActionType.Completed, userId, new DateTime(2026, 5, 8, 10, 0, 0, DateTimeKind.Utc)),
            CreateLog(household.Id, reopenedItemId, ActivityActionType.Uncompleted, userId, new DateTime(2026, 5, 13, 9, 0, 0, DateTimeKind.Utc)),
            CreateLog(household.Id, reopenedItemId, ActivityActionType.Completed, userId, new DateTime(2026, 5, 13, 16, 0, 0, DateTimeKind.Utc)));
        await context.SaveChangesAsync();
        var reader = CreateReader(context);

        var usage = await reader.GetUsageAsync(new DateOnly(2026, 5, 7), new DateOnly(2026, 5, 13));

        var may8 = usage.Days.Single(day => day.Date == new DateOnly(2026, 5, 8));
        may8.ItemsCreated.Should().Be(1);
        may8.ItemsCompleted.Should().Be(2);
        may8.ItemsDeleted.Should().Be(1);
        may8.ItemsUncompleted.Should().Be(0);
        may8.ItemCompletionsFromBacklog.Should().Be(1);

        var may13 = usage.Days.Single(day => day.Date == new DateOnly(2026, 5, 13));
        may13.ItemsCreated.Should().Be(0);
        may13.ItemsCompleted.Should().Be(1);
        may13.ItemsUncompleted.Should().Be(1);
        may13.ItemCompletionsFromBacklog.Should().Be(1);
    }

    [Fact]
    public async Task GetOpenAiAsync_AggregatesUsageByDayAndOperation()
    {
        await using var context = CreateContext();
        var householdId = Guid.NewGuid();
        context.AiUsageEvents.AddRange(
            new AiUsageEvent(householdId, "voice", "transcription", "gpt-4o-mini-transcribe", AiUsageStatus.Success, 1200, 10, 0, null, null, 8, 2, 1.4, 25),
            new AiUsageEvent(householdId, "voice", "parsing", "gpt-5.4-nano", AiUsageStatus.Failure, 900, 100, 20, 50, 3, null, null, null, null));
        await context.SaveChangesAsync();
        var reader = CreateReader(context);

        var usage = await reader.GetOpenAiAsync(DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow));

        usage.Requests.Should().Be(2);
        usage.Failures.Should().Be(1);
        usage.InputTokens.Should().Be(110);
        usage.OutputTokens.Should().Be(20);
        usage.EstimatedCostMicros.Should().Be(25);
        usage.Operations.Should().Contain(operation => operation.Operation == "transcription" && operation.Requests == 1);
        usage.Operations.Should().Contain(operation => operation.Operation == "parsing" && operation.Failures == 1);
    }

    [Fact]
    public async Task GetOverviewAsync_UsesAiUsageEventsForAiSummary()
    {
        await using var context = CreateContext();
        var householdId = Guid.NewGuid();
        context.AiUsageEvents.AddRange(
            new AiUsageEvent(householdId, "voice", "transcription", "gpt-4o-mini-transcribe", AiUsageStatus.Success, 1200, 10, 0, null, null, 8, 2, 1.4, 25),
            new AiUsageEvent(householdId, "voice", "parsing", "gpt-5.4-nano", AiUsageStatus.Failure, 900, 100, 20, 50, 3, null, null, null, 75));
        context.VoiceParseEvents.Add(new VoiceParseEvent(
            userId: Guid.NewGuid(),
            householdId: householdId,
            status: VoiceParseStatus.Success,
            audioSizeBytes: 1,
            audioDurationSeconds: 20,
            parsedItemsCount: 10,
            inputTokens: null,
            outputTokens: null,
            cachedTokens: null,
            reasoningTokens: null,
            estimatedCostMicros: 999,
            latencyMs: 500));
        await context.SaveChangesAsync();
        var reader = CreateReader(context);

        var overview = await reader.GetOverviewAsync(DateTime.UtcNow);

        overview.AiRequests7d.Should().Be(2);
        overview.AiSuccesses7d.Should().Be(1);
        overview.AiFailures7d.Should().Be(1);
        overview.EstimatedAiCostMicros7d.Should().Be(100);
    }

    [Fact]
    public async Task GetMcpOverviewAsync_AggregatesOAuthUsageAndReadinessWithoutSensitiveValues()
    {
        await using var context = CreateContext();
        var userId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var householdId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var now = DateTime.UtcNow;
        var consent = new McpOAuthConsent(userId, "https://chatgpt.com/aip/g-123/.well-known/oauth-client", "https://mcp.convyapp.com", "convy.households.read convy.lists.read");
        var revokedConsent = new McpOAuthConsent(Guid.NewGuid(), "https://chatgpt.com/aip/g-456/.well-known/oauth-client", "https://mcp.convyapp.com", "convy.households.read");
        revokedConsent.Revoke(now);
        var activeRefreshToken = new McpOAuthRefreshToken(
            "active-token-hash-should-never-leak",
            userId,
            consent.ClientId,
            consent.Resource,
            consent.Scopes,
            now.AddDays(3));
        activeRefreshToken.MarkUsed(now.AddMinutes(-5));
        var revokedRefreshToken = new McpOAuthRefreshToken(
            "revoked-token-hash-should-never-leak",
            userId,
            consent.ClientId,
            consent.Resource,
            consent.Scopes,
            now.AddDays(30));
        revokedRefreshToken.Revoke(now);

        context.McpOAuthConsents.AddRange(consent, revokedConsent);
        context.McpOAuthRefreshTokens.AddRange(activeRefreshToken, revokedRefreshToken);
        context.McpToolInvocations.AddRange(
            new McpToolInvocation(userId, householdId, "convy_get_shopping_context", McpToolInvocationStatus.Success, 120, null),
            new McpToolInvocation(userId, householdId, "convy_get_shopping_context", McpToolInvocationStatus.ProviderError, 300, "ProviderError"),
            new McpToolInvocation(userId, null, "convy_get_context", McpToolInvocationStatus.Success, 50, null));
        await context.SaveChangesAsync();
        var reader = CreateReader(context, new StaticHttpClientFactory(new HttpClient(new StaticResponseHandler(HttpStatusCode.OK, "{}"))));

        var overview = await reader.GetMcpOverviewAsync(DateOnly.FromDateTime(now.AddDays(-1)), DateOnly.FromDateTime(now), now);
        var serialized = System.Text.Json.JsonSerializer.Serialize(overview);

        overview.OAuth.ActiveConsents.Should().Be(1);
        overview.OAuth.RevokedConsents.Should().Be(1);
        overview.OAuth.ActiveRefreshTokens.Should().Be(1);
        overview.OAuth.RefreshTokensExpiring7d.Should().Be(1);
        overview.Usage.Invocations.Should().Be(3);
        overview.Usage.Successes.Should().Be(2);
        overview.Usage.Failures.Should().Be(1);
        overview.Usage.P95LatencyMs.Should().Be(300);
        overview.Tools.Should().Contain(tool => tool.ToolName == "convy_get_shopping_context" && tool.Invocations == 2);
        overview.RecentInvocations.Should().OnlyContain(invocation => invocation.UserId.Length == 8);
        overview.Runtime.Scopes.Should().Contain("convy.households.read");
        overview.Runtime.Scopes.Should().Contain("convy.items.write");
        overview.Runtime.Scopes.Should().Contain("convy.tasks.write");
        overview.ToolCatalog.Should().Contain(tool => tool.Name == "convy_add_shopping_items" && !tool.ReadOnlyHint && tool.IdempotentHint);
        overview.ToolCatalog.Should().Contain(tool => tool.Name == "convy_update_tasks_status" && !tool.ReadOnlyHint && tool.IdempotentHint);
        overview.ReadinessChecks.Should().Contain(check => check.Key == "write_scope_safeguards" && check.Status == "Pass");
        serialized.Should().NotContain("active-token-hash-should-never-leak");
        serialized.Should().NotContain("revoked-token-hash-should-never-leak");
        serialized.Should().NotContain(userId.ToString());
    }

    [Fact]
    public void GetMcpOverviewAsync_ShouldAggregateOAuthMetricsInProjectedQueries()
    {
        var source = File.ReadAllText(Path.Combine(FindRepoRoot(), "backend", "src", "Convy.Infrastructure", "Services", "AdminMetricsReader.cs"));

        source.Should().Contain("var consentMetrics = await _context.McpOAuthConsents");
        source.Should().Contain("var refreshTokenMetrics = await _context.McpOAuthRefreshTokens");
        source.Should().Contain(".GroupBy(_ => 1)");
        source.Should().NotContain("var activeConsents = await _context.McpOAuthConsents.AsNoTracking().CountAsync");
        source.Should().NotContain("var latestRefreshTokenRevokedAt = await _context.McpOAuthRefreshTokens.AsNoTracking().MaxAsync");
    }

    private static ConvyDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ConvyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ConvyDbContext(options);
    }

    private static AdminMetricsReader CreateReader(ConvyDbContext context, IHttpClientFactory? httpClientFactory = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Operations:DataPath"] = AppContext.BaseDirectory,
                ["McpAuth:Issuer"] = "https://auth.convyapp.com",
                ["McpAuth:Audience"] = "https://mcp.convyapp.com",
                ["McpAuth:AuthorizationEndpoint"] = "https://auth.convyapp.com/oauth/authorize",
                ["Convy:PublicHostname"] = "convyapp.com",
                ["Convy:LegalHostname"] = "legal.convyapp.com",
            })
            .Build();

        return httpClientFactory is null
            ? new AdminMetricsReader(context, configuration)
            : new AdminMetricsReader(context, configuration, httpClientFactory);
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "backend", "src", "Convy.Infrastructure", "Services", "AdminMetricsReader.cs")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root could not be found.");
    }

    private static ActivityLog CreateLog(Guid householdId, Guid entityId, ActivityActionType action, Guid userId, DateTime createdAt)
    {
        var log = new ActivityLog(householdId, ActivityEntityType.Item, entityId, action, userId);
        SetDate(log, nameof(ActivityLog.CreatedAt), createdAt);
        return log;
    }

    private static void SetId(Entity entity, Guid id) =>
        typeof(Entity).GetProperty(nameof(Entity.Id), BindingFlags.Instance | BindingFlags.Public)!.SetValue(entity, id);

    private static void SetDate<T>(T entity, string propertyName, DateTime value) where T : class =>
        typeof(T).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)!.SetValue(entity, value);

    private sealed class StaticHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;

        public StaticHttpClientFactory(HttpClient client)
        {
            _client = client;
        }

        public HttpClient CreateClient(string name) => _client;
    }

    private sealed class StaticResponseHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _content;

        public StaticResponseHandler(HttpStatusCode statusCode, string content)
        {
            _statusCode = statusCode;
            _content = content;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_content),
            });
    }
}
