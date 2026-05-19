using System.Reflection;
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

    private static ConvyDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ConvyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ConvyDbContext(options);
    }

    private static AdminMetricsReader CreateReader(ConvyDbContext context)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Operations:DataPath"] = AppContext.BaseDirectory,
            })
            .Build();

        return new AdminMetricsReader(context, configuration);
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
}
