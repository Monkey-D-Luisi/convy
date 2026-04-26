using Convy.Infrastructure.Services;
using Convy.Application.Common.Models;
using FluentAssertions;
using NSubstitute;

namespace Convy.Infrastructure.Tests.Services;

public class PushNotificationBatcherTests
{
    [Fact]
    public void EnqueueNotification_MultipleItemsInSameCategory_BatchesTogether()
    {
        var batcher = CreateBatcher();
        var recipientId = Guid.NewGuid();
        var householdId = Guid.NewGuid();
        var listId = Guid.NewGuid();

        batcher.EnqueueNotification([recipientId], householdId, listId, "Luis", "Groceries", "Milk", NotificationCategory.ItemsAdded);
        batcher.EnqueueNotification([recipientId], householdId, listId, "Luis", "Groceries", "Bread", NotificationCategory.ItemsAdded);
        batcher.EnqueueNotification([recipientId], householdId, listId, "Luis", "Groceries", "Eggs", NotificationCategory.ItemsAdded);

        batcher.PendingCount.Should().Be(1);
    }

    [Fact]
    public void EnqueueNotification_DifferentLists_CreatesSeparateBatches()
    {
        var batcher = CreateBatcher();
        var recipientId = Guid.NewGuid();
        var householdId = Guid.NewGuid();
        var listId1 = Guid.NewGuid();
        var listId2 = Guid.NewGuid();

        batcher.EnqueueNotification([recipientId], householdId, listId1, "Luis", "Groceries", "Milk", NotificationCategory.ItemsAdded);
        batcher.EnqueueNotification([recipientId], householdId, listId2, "Luis", "Hardware", "Nails", NotificationCategory.ItemsAdded);

        batcher.PendingCount.Should().Be(2);
    }

    [Fact]
    public void EnqueueNotification_DifferentCategories_CreatesSeparateBatches()
    {
        var batcher = CreateBatcher();
        var recipientId = Guid.NewGuid();
        var householdId = Guid.NewGuid();
        var listId = Guid.NewGuid();

        batcher.EnqueueNotification([recipientId], householdId, listId, "Luis", "Groceries", "Milk", NotificationCategory.ItemsAdded);
        batcher.EnqueueNotification([recipientId], householdId, listId, "Luis", "Groceries", "Milk", NotificationCategory.ItemsCompleted);

        batcher.PendingCount.Should().Be(2);
    }

    [Fact]
    public void CreateTemplate_ForTaskCompletionBatch_ReturnsTaskCompletionTemplate()
    {
        var entry = new PushNotificationBatcher.BatchEntry(
            recipientUserIds: [Guid.NewGuid()],
            actorName: "Luis",
            listName: "Home",
            entryTitles: ["Clean kitchen", "Pay bills"],
            category: NotificationCategory.TasksCompleted,
            data: null,
            lastEnqueuedAt: DateTime.UtcNow);

        var template = PushNotificationBatcher.CreateTemplate(entry);

        template.Key.Should().Be(NotificationTemplateKey.TasksCompleted);
        template.Parameters["actorName"].Should().Be("Luis");
        template.Parameters["listName"].Should().Be("Home");
        template.Parameters["count"].Should().Be("2");
    }

    [Fact]
    public void BatchEntry_Append_ReturnsNewSnapshotWithoutMutatingExistingEntry()
    {
        var firstRecipientId = Guid.NewGuid();
        var secondRecipientId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 4, 17, 10, 0, 0, TimeSpan.Zero);
        var later = now.AddSeconds(5);
        var entry = new PushNotificationBatcher.BatchEntry(
            recipientUserIds: [firstRecipientId],
            actorName: "Luis",
            listName: "Groceries",
            entryTitles: ["Milk"],
            category: NotificationCategory.ItemsAdded,
            data: new Dictionary<string, string> { ["listId"] = "list-1" },
            lastEnqueuedAt: now);

        var appended = entry.Append([secondRecipientId], "Bread", later);

        entry.EntryTitles.Should().ContainSingle().Which.Should().Be("Milk");
        entry.RecipientUserIds.Should().ContainSingle().Which.Should().Be(firstRecipientId);
        entry.LastEnqueuedAt.Should().Be(now);

        appended.EntryTitles.Should().ContainInOrder("Milk", "Bread");
        appended.RecipientUserIds.Should().BeEquivalentTo([firstRecipientId, secondRecipientId]);
        appended.LastEnqueuedAt.Should().Be(later);
    }

    [Fact]
    public void EnqueueNotification_UsesInjectedTimeProvider()
    {
        var now = new DateTimeOffset(2026, 4, 17, 11, 0, 0, TimeSpan.Zero);
        var timeProvider = new TestTimeProvider(now);
        var batcher = CreateBatcher(timeProvider);
        var householdId = Guid.NewGuid();
        var listId = Guid.NewGuid();

        batcher.EnqueueNotification([Guid.NewGuid()], householdId, listId, "Luis", "Groceries", "Milk", NotificationCategory.ItemsAdded);

        var entry = batcher.GetPendingEntryForTests(householdId, listId);
        entry!.LastEnqueuedAt.Should().Be(now);
    }

    private static PushNotificationBatcher CreateBatcher(TimeProvider? timeProvider = null)
    {
        var config = Substitute.For<Microsoft.Extensions.Configuration.IConfiguration>();
        config[Arg.Is("PushNotifications:BatchWindowSeconds")].Returns("60");

        var scopeFactory = Substitute.For<Microsoft.Extensions.DependencyInjection.IServiceScopeFactory>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<PushNotificationBatcher>>();

        return new PushNotificationBatcher(scopeFactory, config, logger, timeProvider ?? TimeProvider.System);
    }

    private sealed class TestTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public TestTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow() => _utcNow;
    }
}
