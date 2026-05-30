using Convy.Application.Common.Interfaces;
using Convy.Application.Common.Models;
using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Convy.Infrastructure.Services;

public class TaskReminderService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(5);
    internal const int BatchSize = 100;

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TaskReminderService> _logger;

    public TaskReminderService(IServiceProvider serviceProvider, ILogger<TaskReminderService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDueRemindersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing task reminders");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    public async Task ProcessDueRemindersAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var taskRepository = scope.ServiceProvider.GetRequiredService<ITaskItemRepository>();
        var listRepository = scope.ServiceProvider.GetRequiredService<IHouseholdListRepository>();
        var householdRepository = scope.ServiceProvider.GetRequiredService<IHouseholdRepository>();
        var pushService = scope.ServiceProvider.GetRequiredService<IPushNotificationService>();
        var processingLock = scope.ServiceProvider.GetRequiredService<ITaskReminderProcessingLock>();

        await using var acquiredLock = await processingLock.TryAcquireAsync(cancellationToken);
        if (acquiredLock is null)
        {
            _logger.LogInformation("Task reminder processing skipped because another instance holds the lock");
            return;
        }

        var now = DateTime.UtcNow;
        var dueTasks = await taskRepository.GetDueRemindersAsync(now, BatchSize, cancellationToken);
        var listCache = new Dictionary<Guid, HouseholdList?>();
        var householdCache = new Dictionary<Guid, Household?>();

        foreach (var task in dueTasks)
        {
            try
            {
                if (task.IsCompleted)
                    continue;

                var list = await GetListAsync(task.ListId, listRepository, listCache, cancellationToken);
                if (list is null)
                {
                    _logger.LogWarning("Skipping task reminder because list was not found taskId={TaskId} listId={ListId}", task.Id, task.ListId);
                    continue;
                }

                var household = await GetHouseholdAsync(list.HouseholdId, householdRepository, householdCache, cancellationToken);
                if (household is null)
                {
                    _logger.LogWarning("Skipping task reminder because household was not found taskId={TaskId} householdId={HouseholdId}", task.Id, list.HouseholdId);
                    continue;
                }

                var recipients = ResolveRecipients(task, household);
                if (recipients.Count == 0)
                {
                    _logger.LogWarning("Skipping task reminder because no recipients were resolved taskId={TaskId}", task.Id);
                    continue;
                }

                await pushService.SendLocalizedAsync(
                    recipients,
                    NotificationCategory.TaskReminders,
                    new PushNotificationTemplate(
                        NotificationTemplateKey.TaskReminderDue,
                        new Dictionary<string, string>
                        {
                            ["title"] = task.Title,
                            ["listName"] = list.Name
                        }),
                    new Dictionary<string, string>
                    {
                        ["type"] = "task-reminder",
                        ["listId"] = list.Id.ToString(),
                        ["taskId"] = task.Id.ToString()
                    },
                    cancellationToken);

                task.MarkReminderSent(now);
                await taskRepository.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Failed to process task reminder taskId={TaskId}", task.Id);
            }
        }
    }

    private static async Task<HouseholdList?> GetListAsync(
        Guid listId,
        IHouseholdListRepository repository,
        Dictionary<Guid, HouseholdList?> cache,
        CancellationToken cancellationToken)
    {
        if (!cache.TryGetValue(listId, out var list))
        {
            list = await repository.GetByIdAsync(listId, cancellationToken);
            cache[listId] = list;
        }

        return list;
    }

    private static async Task<Household?> GetHouseholdAsync(
        Guid householdId,
        IHouseholdRepository repository,
        Dictionary<Guid, Household?> cache,
        CancellationToken cancellationToken)
    {
        if (!cache.TryGetValue(householdId, out var household))
        {
            household = await repository.GetByIdWithMembersAsync(householdId, cancellationToken);
            cache[householdId] = household;
        }

        return household;
    }

    private static IReadOnlyList<Guid> ResolveRecipients(TaskItem task, Household household)
    {
        if (task.AssignedToUserId.HasValue)
            return household.IsMember(task.AssignedToUserId.Value) ? [task.AssignedToUserId.Value] : [];

        return household.Memberships.Select(member => member.UserId).Distinct().ToList();
    }
}
