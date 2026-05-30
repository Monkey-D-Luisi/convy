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
                Arg.Any<int>(),
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
        await taskRepository.Received(1).GetDueRemindersAsync(
            Arg.Any<DateTime>(),
            100,
            Arg.Any<CancellationToken>());
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
        taskRepository.GetDueRemindersAsync(Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
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

    [Fact]
    public async Task ProcessDueRemindersAsync_WhenAnotherExecutionHasLock_DoesNotSendDuplicateReminder()
    {
        var creatorId = Guid.NewGuid();
        var household = new Household("Home", creatorId);
        var list = new HouseholdList("Chores", ListType.Tasks, household.Id, creatorId);
        var task = new TaskItem(
            "Clean kitchen",
            list.Id,
            creatorId,
            reminderAtUtc: new DateTime(2026, 5, 30, 7, 0, 0, DateTimeKind.Utc));
        var taskRepository = Substitute.For<ITaskItemRepository>();
        taskRepository.GetDueRemindersAsync(Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([task]);
        var listRepository = Substitute.For<IHouseholdListRepository>();
        listRepository.GetByIdAsync(list.Id, Arg.Any<CancellationToken>()).Returns(list);
        var householdRepository = Substitute.For<IHouseholdRepository>();
        householdRepository.GetByIdWithMembersAsync(household.Id, Arg.Any<CancellationToken>()).Returns(household);
        var push = Substitute.For<IPushNotificationService>();
        var reminderLock = new SingleEntryTaskReminderProcessingLock();
        var service = CreateService(taskRepository, listRepository, householdRepository, push, reminderLock);

        await Task.WhenAll(
            service.ProcessDueRemindersAsync(CancellationToken.None),
            service.ProcessDueRemindersAsync(CancellationToken.None));

        await push.Received(1).SendLocalizedAsync(
            Arg.Any<IEnumerable<Guid>>(),
            NotificationCategory.TaskReminders,
            Arg.Any<PushNotificationTemplate>(),
            Arg.Any<Dictionary<string, string>?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessDueRemindersAsync_WhenOnePushFails_KeepsSuccessfulMarksAndContinues()
    {
        var creatorId = Guid.NewGuid();
        var household = new Household("Home", creatorId);
        var list = new HouseholdList("Chores", ListType.Tasks, household.Id, creatorId);
        var first = new TaskItem("Clean kitchen", list.Id, creatorId, reminderAtUtc: DateTime.UtcNow.AddMinutes(-5));
        var second = new TaskItem("Take trash out", list.Id, creatorId, reminderAtUtc: DateTime.UtcNow.AddMinutes(-4));
        var taskRepository = Substitute.For<ITaskItemRepository>();
        taskRepository.GetDueRemindersAsync(Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([first, second]);
        var listRepository = Substitute.For<IHouseholdListRepository>();
        listRepository.GetByIdAsync(list.Id, Arg.Any<CancellationToken>()).Returns(list);
        var householdRepository = Substitute.For<IHouseholdRepository>();
        householdRepository.GetByIdWithMembersAsync(household.Id, Arg.Any<CancellationToken>()).Returns(household);
        var push = Substitute.For<IPushNotificationService>();
        push.SendLocalizedAsync(
                Arg.Any<IEnumerable<Guid>>(),
                NotificationCategory.TaskReminders,
                Arg.Is<PushNotificationTemplate>(template => template.Parameters["title"] == second.Title),
                Arg.Any<Dictionary<string, string>?>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException(new InvalidOperationException("push failed")));

        var service = CreateService(taskRepository, listRepository, householdRepository, push);

        await service.ProcessDueRemindersAsync(CancellationToken.None);

        first.ReminderSentAtUtc.Should().NotBeNull();
        second.ReminderSentAtUtc.Should().BeNull();
        await taskRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await push.Received(2).SendLocalizedAsync(
            Arg.Any<IEnumerable<Guid>>(),
            NotificationCategory.TaskReminders,
            Arg.Any<PushNotificationTemplate>(),
            Arg.Any<Dictionary<string, string>?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessDueRemindersAsync_CachesListsAndHouseholdsWithinBatch()
    {
        var creatorId = Guid.NewGuid();
        var household = new Household("Home", creatorId);
        var list = new HouseholdList("Chores", ListType.Tasks, household.Id, creatorId);
        var first = new TaskItem("Clean kitchen", list.Id, creatorId, reminderAtUtc: DateTime.UtcNow.AddMinutes(-5));
        var second = new TaskItem("Take trash out", list.Id, creatorId, reminderAtUtc: DateTime.UtcNow.AddMinutes(-4));
        var taskRepository = Substitute.For<ITaskItemRepository>();
        taskRepository.GetDueRemindersAsync(Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([first, second]);
        var listRepository = Substitute.For<IHouseholdListRepository>();
        listRepository.GetByIdAsync(list.Id, Arg.Any<CancellationToken>()).Returns(list);
        var householdRepository = Substitute.For<IHouseholdRepository>();
        householdRepository.GetByIdWithMembersAsync(household.Id, Arg.Any<CancellationToken>()).Returns(household);

        var service = CreateService(taskRepository, listRepository, householdRepository, Substitute.For<IPushNotificationService>());

        await service.ProcessDueRemindersAsync(CancellationToken.None);

        await listRepository.Received(1).GetByIdAsync(list.Id, Arg.Any<CancellationToken>());
        await householdRepository.Received(1).GetByIdWithMembersAsync(household.Id, Arg.Any<CancellationToken>());
    }

    private static TaskReminderService CreateService(
        ITaskItemRepository taskRepository,
        IHouseholdListRepository listRepository,
        IHouseholdRepository householdRepository,
        IPushNotificationService push,
        ITaskReminderProcessingLock? reminderLock = null)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => taskRepository);
        services.AddScoped(_ => listRepository);
        services.AddScoped(_ => householdRepository);
        services.AddScoped(_ => push);
        services.AddScoped(_ => reminderLock ?? new AlwaysAvailableTaskReminderProcessingLock());
        return new TaskReminderService(services.BuildServiceProvider(), NullLogger<TaskReminderService>.Instance);
    }

    private sealed class AlwaysAvailableTaskReminderProcessingLock : ITaskReminderProcessingLock
    {
        public Task<IAsyncDisposable?> TryAcquireAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IAsyncDisposable?>(new Releaser());
    }

    private sealed class SingleEntryTaskReminderProcessingLock : ITaskReminderProcessingLock
    {
        private int _entered;

        public Task<IAsyncDisposable?> TryAcquireAsync(CancellationToken cancellationToken)
        {
            return Interlocked.Exchange(ref _entered, 1) == 0
                ? Task.FromResult<IAsyncDisposable?>(new Releaser())
                : Task.FromResult<IAsyncDisposable?>(null);
        }
    }

    private sealed class Releaser : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
