using System.Collections.Concurrent;
using Convy.Application.Common.Interfaces;
using Convy.Application.Common.Models;
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
    private readonly ConcurrentDictionary<BatchKey, BatchEntry> _pending = new();

    internal int PendingCount => _pending.Count;
    internal BatchEntry? GetPendingEntryForTests(Guid householdId, Guid listId) =>
        _pending.FirstOrDefault(p => p.Key.HouseholdId == householdId && p.Key.ListId == listId).Value;

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

    public void EnqueueNotification(
        IEnumerable<Guid> recipientUserIds,
        Guid householdId,
        Guid listId,
        string actorName,
        string listName,
        string entryTitle,
        NotificationCategory category,
        Dictionary<string, string>? data = null)
    {
        var key = new BatchKey(householdId, listId, actorName, category);
        var recipientSet = recipientUserIds.ToHashSet();
        var now = _timeProvider.GetUtcNow();

        _pending.AddOrUpdate(
            key,
            _ => new BatchEntry(recipientSet, actorName, listName, [entryTitle], category, data, now),
            (_, existing) => existing.Append(recipientSet, entryTitle, now, data));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PushNotificationBatcher started with {Window}s batch window", _batchWindow.TotalSeconds);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                await FlushExpiredBatchesAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }

        await FlushExpiredBatchesAsync(CancellationToken.None, force: true);
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
                using var scope = _scopeFactory.CreateScope();
                var pushService = scope.ServiceProvider.GetRequiredService<IPushNotificationService>();

                await pushService.SendLocalizedAsync(
                    entry.RecipientUserIds,
                    entry.Category,
                    CreateTemplate(entry),
                    entry.Data is null ? null : new Dictionary<string, string>(entry.Data),
                    cancellationToken);

                _logger.LogInformation(
                    "Flushed batched push: {Count} entries for list {ListId} in household {HouseholdId} category {Category}",
                    entry.EntryTitles.Count, key.ListId, key.HouseholdId, entry.Category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to flush batched push for list {ListId} in household {HouseholdId} category {Category}",
                    key.ListId, key.HouseholdId, entry.Category);
            }
        }
    }

    private bool TryRemoveExact(BatchKey key, BatchEntry entry)
    {
        var collection = (ICollection<KeyValuePair<BatchKey, BatchEntry>>)_pending;
        return collection.Remove(new KeyValuePair<BatchKey, BatchEntry>(key, entry));
    }

    internal static PushNotificationTemplate CreateTemplate(BatchEntry entry)
    {
        var key = entry.Category switch
        {
            NotificationCategory.TasksAdded => NotificationTemplateKey.TasksAdded,
            NotificationCategory.ItemsCompleted => NotificationTemplateKey.ItemsCompleted,
            NotificationCategory.TasksCompleted => NotificationTemplateKey.TasksCompleted,
            _ => NotificationTemplateKey.ItemsAdded
        };

        return new PushNotificationTemplate(
            key,
            new Dictionary<string, string>
            {
                ["actorName"] = entry.ActorName,
                ["listName"] = entry.ListName,
                ["count"] = entry.EntryTitles.Count.ToString()
            });
    }

    private sealed record BatchKey(Guid HouseholdId, Guid ListId, string ActorName, NotificationCategory Category);

    internal class BatchEntry
    {
        public IReadOnlyCollection<Guid> RecipientUserIds { get; }
        public string ActorName { get; }
        public string ListName { get; }
        public IReadOnlyList<string> EntryTitles { get; }
        public NotificationCategory Category { get; }
        public IReadOnlyDictionary<string, string>? Data { get; }
        public DateTimeOffset LastEnqueuedAt { get; }

        public BatchEntry(
            IEnumerable<Guid> recipientUserIds,
            string actorName,
            string listName,
            IEnumerable<string> entryTitles,
            NotificationCategory category,
            Dictionary<string, string>? data,
            DateTimeOffset lastEnqueuedAt)
        {
            RecipientUserIds = recipientUserIds.ToHashSet();
            ActorName = actorName;
            ListName = listName;
            EntryTitles = entryTitles.ToList();
            Category = category;
            Data = data is null ? null : new Dictionary<string, string>(data);
            LastEnqueuedAt = lastEnqueuedAt;
        }

        public BatchEntry Append(
            IEnumerable<Guid> recipientUserIds,
            string entryTitle,
            DateTimeOffset lastEnqueuedAt,
            Dictionary<string, string>? data = null)
        {
            var recipients = RecipientUserIds.Concat(recipientUserIds).ToHashSet();
            var entryTitles = EntryTitles.Concat([entryTitle]).ToList();
            var snapshotData = data ?? (Data is null ? null : new Dictionary<string, string>(Data));

            return new BatchEntry(recipients, ActorName, ListName, entryTitles, Category, snapshotData, lastEnqueuedAt);
        }
    }
}
