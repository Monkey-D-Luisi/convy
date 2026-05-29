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

        var now = DateTime.UtcNow;
        var dueTasks = await taskRepository.GetDueRemindersAsync(now, cancellationToken);

        foreach (var task in dueTasks)
        {
            if (task.IsCompleted)
                continue;

            var list = await listRepository.GetByIdAsync(task.ListId, cancellationToken);
            if (list is null)
                continue;

            var household = await householdRepository.GetByIdWithMembersAsync(list.HouseholdId, cancellationToken);
            if (household is null)
                continue;

            var recipients = ResolveRecipients(task, household);
            if (recipients.Count == 0)
                continue;

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
        }

        if (dueTasks.Count > 0)
            await taskRepository.SaveChangesAsync(cancellationToken);
    }

    private static IReadOnlyList<Guid> ResolveRecipients(TaskItem task, Household household)
    {
        if (task.AssignedToUserId.HasValue)
            return household.IsMember(task.AssignedToUserId.Value) ? [task.AssignedToUserId.Value] : [];

        return household.Memberships.Select(member => member.UserId).Distinct().ToList();
    }
}
