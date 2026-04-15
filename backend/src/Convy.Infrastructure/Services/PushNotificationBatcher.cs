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
    private readonly TimeSpan _batchWindow;
    private readonly ConcurrentDictionary<(Guid HouseholdId, Guid ListId), BatchEntry> _pending = new();

    internal int PendingCount => _pending.Count;

    public PushNotificationBatcher(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<PushNotificationBatcher> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

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

        _pending.AddOrUpdate(
            key,
            _ => new BatchEntry(recipientSet, listName, [itemTitle], data, DateTime.UtcNow),
            (_, existing) =>
            {
                lock (existing)
                {
                    foreach (var id in recipientSet)
                        existing.RecipientUserIds.Add(id);

                    existing.ItemTitles.Add(itemTitle);
                    existing.LastEnqueuedAt = DateTime.UtcNow;

                    if (data is not null)
                        existing.Data = data;
                }

                return existing;
            });
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
        var now = DateTime.UtcNow;

        foreach (var key in _pending.Keys)
        {
            if (!_pending.TryGetValue(key, out var entry))
                continue;

            bool shouldFlush;
            lock (entry)
            {
                shouldFlush = force || (now - entry.LastEnqueuedAt) >= _batchWindow;
            }

            if (!shouldFlush)
                continue;

            if (!_pending.TryRemove(key, out var removed))
                continue;

            try
            {
                var (title, body) = ComposeMessage(removed);

                using var scope = _scopeFactory.CreateScope();
                var pushService = scope.ServiceProvider.GetRequiredService<IPushNotificationService>();

                await pushService.SendToUsersAsync(
                    removed.RecipientUserIds,
                    title,
                    body,
                    removed.Data,
                    cancellationToken);

                _logger.LogInformation(
                    "Flushed batched push: {Count} items for list {ListId} in household {HouseholdId}",
                    removed.ItemTitles.Count, key.ListId, key.HouseholdId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to flush batched push for list {ListId} in household {HouseholdId}",
                    key.ListId, key.HouseholdId);
            }
        }
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
        public HashSet<Guid> RecipientUserIds { get; }
        public string ListName { get; }
        public List<string> ItemTitles { get; }
        public Dictionary<string, string>? Data { get; set; }
        public DateTime LastEnqueuedAt { get; set; }

        public BatchEntry(
            HashSet<Guid> recipientUserIds,
            string listName,
            List<string> itemTitles,
            Dictionary<string, string>? data,
            DateTime lastEnqueuedAt)
        {
            RecipientUserIds = recipientUserIds;
            ListName = listName;
            ItemTitles = itemTitles;
            Data = data;
            LastEnqueuedAt = lastEnqueuedAt;
        }
    }
}
