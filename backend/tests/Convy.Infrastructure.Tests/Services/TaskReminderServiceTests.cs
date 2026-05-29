using Convy.Application.Common.Interfaces;
using Convy.Application.Common.Models;
using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using Convy.Domain.ValueObjects;
using Convy.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Convy.Infrastructure.Tests.Services;

public class TaskReminderServiceTests
{
    [Fact]
    public async Task ProcessDueRemindersAsync_WhenAssignedTaskIsDue_SendsToAssigneeAndMarksSent()
    {
        var creatorId = Guid.NewGuid();
        var assigneeId = Guid.NewGuid();
        var household = new Household("Home", creatorId);
        household.AddMember(assigneeId);
        var list = new HouseholdList("Chores", ListType.Tasks, household.Id, creatorId);
        var task = new TaskItem(
            "Clean kitchen",
            list.Id,
            creatorId,
            assignedToUserId: assigneeId,
            dueDate: new DateOnly(2026, 5, 30),
            reminderAtUtc: new DateTime(2026, 5, 30, 7, 0, 0, DateTimeKind.Utc),
            priority: TaskPriority.High);
        var taskRepository = Substitute.For<ITaskItemRepository>();
        taskRepository.GetDueRemindersAsync(
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns([task]);
        var listRepository = Substitute.For<IHouseholdListRepository>();
        listRepository.GetByIdAsync(list.Id, Arg.Any<CancellationToken>()).Returns(list);
        var householdRepository = Substitute.For<IHouseholdRepository>();
        householdRepository.GetByIdWithMembersAsync(household.Id, Arg.Any<CancellationToken>()).Returns(household);
        var push = Substitute.For<IPushNotificationService>();

        var service = CreateService(taskRepository, listRepository, householdRepository, push);

        await service.ProcessDueRemindersAsync(CancellationToken.None);

        await push.Received(1).SendLocalizedAsync(
            Arg.Is<IEnumerable<Guid>>(ids => ids.SequenceEqual(new[] { assigneeId })),
            NotificationCategory.TaskReminders,
            Arg.Is<PushNotificationTemplate>(template => template.Key == NotificationTemplateKey.TaskReminderDue),
            Arg.Is<Dictionary<string, string>>(data =>
                data["type"] == "task-reminder" &&
                data["listId"] == list.Id.ToString() &&
                data["taskId"] == task.Id.ToString()),
            Arg.Any<CancellationToken>());
        task.ReminderSentAtUtc.Should().NotBeNull();
        await taskRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessDueRemindersAsync_WhenTaskIsCompleted_DoesNotSendReminder()
    {
        var creatorId = Guid.NewGuid();
        var household = new Household("Home", creatorId);
        var list = new HouseholdList("Chores", ListType.Tasks, household.Id, creatorId);
        var task = new TaskItem(
            "Clean kitchen",
            list.Id,
            creatorId,
            reminderAtUtc: new DateTime(2026, 5, 30, 7, 0, 0, DateTimeKind.Utc));
        task.Complete(creatorId);
        var taskRepository = Substitute.For<ITaskItemRepository>();
        taskRepository.GetDueRemindersAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns([task]);
        var push = Substitute.For<IPushNotificationService>();

        var service = CreateService(
            taskRepository,
            Substitute.For<IHouseholdListRepository>(),
            Substitute.For<IHouseholdRepository>(),
            push);

        await service.ProcessDueRemindersAsync(CancellationToken.None);

        await push.DidNotReceive().SendLocalizedAsync(
            Arg.Any<IEnumerable<Guid>>(),
            Arg.Any<NotificationCategory>(),
            Arg.Any<PushNotificationTemplate>(),
            Arg.Any<Dictionary<string, string>?>(),
            Arg.Any<CancellationToken>());
        task.ReminderSentAtUtc.Should().BeNull();
    }

    private static TaskReminderService CreateService(
        ITaskItemRepository taskRepository,
        IHouseholdListRepository listRepository,
        IHouseholdRepository householdRepository,
        IPushNotificationService push)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => taskRepository);
        services.AddScoped(_ => listRepository);
        services.AddScoped(_ => householdRepository);
        services.AddScoped(_ => push);
        return new TaskReminderService(services.BuildServiceProvider(), NullLogger<TaskReminderService>.Instance);
    }
}
