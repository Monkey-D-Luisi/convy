using System.Collections.Concurrent;
using Convy.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Convy.Infrastructure.Services;

public class PushNotificationBatcher : BackgroundService, IPushNotificationBatcher
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PushNotificationBatcher> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _batchWindow;
    private readonly ConcurrentDictionary<(Guid HouseholdId, Guid ListId), BatchEntry> _pending = new();

    internal int PendingCount => _pending.Count;
    internal BatchEntry? GetPendingEntryForTests(Guid householdId, Guid listId) =>
        _pending.TryGetValue((householdId, listId), out var entry) ? entry : null;

    public PushNotificationBatcher(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<PushNotificationBatcher> logger,
        TimeProvider? timeProvider = null)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;

        var raw = configuration["PushNotifications:BatchWindowSeconds"];
        var seconds = int.TryParse(raw, out var val) ? val : 60;
        _batchWindow = TimeSpan.FromSeconds(seconds);
    }

    public void EnqueueItemNotification(
        IEnumerable<Guid> recipientUserIds,
        Guid householdId,
        Guid listId,
        string listName,
        string itemTitle,
        Dictionary<string, string>? data = null)
    {
        var key = (householdId, listId);
        var recipientSet = recipientUserIds.ToHashSet();
        var now = _timeProvider.GetUtcNow();

        _pending.AddOrUpdate(
            key,
            _ => new BatchEntry(recipientSet, listName, [itemTitle], data, now),
            (_, existing) => existing.Append(recipientSet, itemTitle, now, data));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PushNotificationBatcher started with {Window}s batch window", _batchWindow.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            await FlushExpiredBatchesAsync(stoppingToken);
        }

        // Flush remaining on shutdown
        await FlushExpiredBatchesAsync(stoppingToken, force: true);
    }

    private async Task FlushExpiredBatchesAsync(CancellationToken cancellationToken, bool force = false)
    {
        var now = _timeProvider.GetUtcNow();

        foreach (var (key, entry) in _pending.ToArray())
        {
            if (!force && (now - entry.LastEnqueuedAt) < _batchWindow)
                continue;

            if (!TryRemoveExact(key, entry))
                continue;

            try
            {
                var (title, body) = ComposeMessage(entry);

                using var scope = _scopeFactory.CreateScope();
                var pushService = scope.ServiceProvider.GetRequiredService<IPushNotificationService>();

                await pushService.SendToUsersAsync(
                    entry.RecipientUserIds,
                    title,
                    body,
                    entry.Data is null ? null : new Dictionary<string, string>(entry.Data),
                    cancellationToken);

                _logger.LogInformation(
                    "Flushed batched push: {Count} items for list {ListId} in household {HouseholdId}",
                    entry.ItemTitles.Count, key.ListId, key.HouseholdId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to flush batched push for list {ListId} in household {HouseholdId}",
                    key.ListId, key.HouseholdId);
            }
        }
    }

    private bool TryRemoveExact((Guid HouseholdId, Guid ListId) key, BatchEntry entry)
    {
        var collection = (ICollection<KeyValuePair<(Guid HouseholdId, Guid ListId), BatchEntry>>)_pending;
        return collection.Remove(new KeyValuePair<(Guid HouseholdId, Guid ListId), BatchEntry>(key, entry));
    }

    internal static (string Title, string Body) ComposeMessage(BatchEntry entry)
    {
        var count = entry.ItemTitles.Count;

        if (count == 1)
            return ("New item added", $"{entry.ItemTitles[0]} was added to {entry.ListName}");

        const int maxDisplay = 2;
        var displayed = entry.ItemTitles.Take(maxDisplay).ToList();
        var remaining = count - maxDisplay;

        var itemsPart = string.Join(", ", displayed);
        if (remaining > 0)
            itemsPart += $" and {remaining} more";

        return ("Items added", $"{itemsPart} were added to {entry.ListName}");
    }

    internal class BatchEntry
    {
        public IReadOnlyCollection<Guid> RecipientUserIds { get; }
        public string ListName { get; }
        public IReadOnlyList<string> ItemTitles { get; }
        public IReadOnlyDictionary<string, string>? Data { get; }
        public DateTimeOffset LastEnqueuedAt { get; }

        public BatchEntry(
            IEnumerable<Guid> recipientUserIds,
            string listName,
            IEnumerable<string> itemTitles,
            Dictionary<string, string>? data,
            DateTimeOffset lastEnqueuedAt)
        {
            RecipientUserIds = recipientUserIds.ToHashSet();
            ListName = listName;
            ItemTitles = itemTitles.ToList();
            Data = data is null ? null : new Dictionary<string, string>(data);
            LastEnqueuedAt = lastEnqueuedAt;
        }

        public BatchEntry Append(
            IEnumerable<Guid> recipientUserIds,
            string itemTitle,
            DateTimeOffset lastEnqueuedAt,
            Dictionary<string, string>? data = null)
        {
            var recipients = RecipientUserIds.Concat(recipientUserIds).ToHashSet();
            var itemTitles = ItemTitles.Concat([itemTitle]).ToList();
            var snapshotData = data ?? (Data is null ? null : new Dictionary<string, string>(Data));

            return new BatchEntry(recipients, ListName, itemTitles, snapshotData, lastEnqueuedAt);
        }
    }
}
