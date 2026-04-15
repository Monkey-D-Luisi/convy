using Convy.Infrastructure.Services;
using FluentAssertions;
using NSubstitute;

namespace Convy.Infrastructure.Tests.Services;

public class PushNotificationBatcherTests
{
    [Fact]
    public void ComposeMessage_SingleItem_ReturnsSingularMessage()
    {
        var entry = new PushNotificationBatcher.BatchEntry(
            recipientUserIds: [Guid.NewGuid()],
            listName: "Groceries",
            itemTitles: ["Milk"],
            data: null,
            lastEnqueuedAt: DateTime.UtcNow);

        var (title, body) = PushNotificationBatcher.ComposeMessage(entry);

        title.Should().Be("New item added");
        body.Should().Be("Milk was added to Groceries");
    }

    [Fact]
    public void ComposeMessage_TwoItems_ReturnsCommaSeparated()
    {
        var entry = new PushNotificationBatcher.BatchEntry(
            recipientUserIds: [Guid.NewGuid()],
            listName: "Groceries",
            itemTitles: ["Milk", "Bread"],
            data: null,
            lastEnqueuedAt: DateTime.UtcNow);

        var (title, body) = PushNotificationBatcher.ComposeMessage(entry);

        title.Should().Be("Items added");
        body.Should().Be("Milk, Bread were added to Groceries");
    }

    [Fact]
    public void ComposeMessage_FourItems_ShowsTwoAndMore()
    {
        var entry = new PushNotificationBatcher.BatchEntry(
            recipientUserIds: [Guid.NewGuid()],
            listName: "Groceries",
            itemTitles: ["Milk", "Bread", "Eggs", "Butter"],
            data: null,
            lastEnqueuedAt: DateTime.UtcNow);

        var (title, body) = PushNotificationBatcher.ComposeMessage(entry);

        title.Should().Be("Items added");
        body.Should().Be("Milk, Bread and 2 more were added to Groceries");
    }

    [Fact]
    public void ComposeMessage_ThreeItems_ShowsTwoAndOneMore()
    {
        var entry = new PushNotificationBatcher.BatchEntry(
            recipientUserIds: [Guid.NewGuid()],
            listName: "Shopping",
            itemTitles: ["Milk", "Bread", "Eggs"],
            data: null,
            lastEnqueuedAt: DateTime.UtcNow);

        var (title, body) = PushNotificationBatcher.ComposeMessage(entry);

        title.Should().Be("Items added");
        body.Should().Be("Milk, Bread and 1 more were added to Shopping");
    }

    [Fact]
    public void EnqueueItemNotification_MultipleItems_BatchesTogether()
    {
        var batcher = CreateBatcher();
        var recipientId = Guid.NewGuid();
        var householdId = Guid.NewGuid();
        var listId = Guid.NewGuid();

        batcher.EnqueueItemNotification([recipientId], householdId, listId, "Groceries", "Milk");
        batcher.EnqueueItemNotification([recipientId], householdId, listId, "Groceries", "Bread");
        batcher.EnqueueItemNotification([recipientId], householdId, listId, "Groceries", "Eggs");

        batcher.PendingCount.Should().Be(1);
    }

    [Fact]
    public void EnqueueItemNotification_DifferentLists_CreatesSeparateBatches()
    {
        var batcher = CreateBatcher();
        var recipientId = Guid.NewGuid();
        var householdId = Guid.NewGuid();
        var listId1 = Guid.NewGuid();
        var listId2 = Guid.NewGuid();

        batcher.EnqueueItemNotification([recipientId], householdId, listId1, "Groceries", "Milk");
        batcher.EnqueueItemNotification([recipientId], householdId, listId2, "Hardware", "Nails");

        batcher.PendingCount.Should().Be(2);
    }

    private static PushNotificationBatcher CreateBatcher()
    {
        var config = Substitute.For<Microsoft.Extensions.Configuration.IConfiguration>();
        config[Arg.Is("PushNotifications:BatchWindowSeconds")].Returns("60");

        var scopeFactory = Substitute.For<Microsoft.Extensions.DependencyInjection.IServiceScopeFactory>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<PushNotificationBatcher>>();

        return new PushNotificationBatcher(scopeFactory, config, logger);
    }
}
