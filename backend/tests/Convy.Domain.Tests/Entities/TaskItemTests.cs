using Convy.Domain.Entities;
using Convy.Domain.ValueObjects;
using FluentAssertions;

namespace Convy.Domain.Tests.Entities;

public class TaskItemTests
{
    private readonly Guid _listId = Guid.NewGuid();
    private readonly Guid _creatorId = Guid.NewGuid();

    [Fact]
    public void Constructor_WithTitleOnly_CreatesTask()
    {
        var task = new TaskItem("Clean kitchen", _listId, _creatorId);

        task.Title.Should().Be("Clean kitchen");
        task.Note.Should().BeNull();
        task.ListId.Should().Be(_listId);
        task.CreatedBy.Should().Be(_creatorId);
        task.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        task.IsCompleted.Should().BeFalse();
        task.CompletedBy.Should().BeNull();
        task.CompletedAt.Should().BeNull();
        task.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Constructor_WithNote_CreatesTask()
    {
        var task = new TaskItem("Clean kitchen", _listId, _creatorId, "Before dinner");

        task.Note.Should().Be("Before dinner");
    }

    [Fact]
    public void Constructor_WithStructuredTaskFields_CreatesTask()
    {
        var assigneeId = Guid.NewGuid();
        var dueDate = new DateOnly(2026, 5, 30);
        var reminderAtUtc = new DateTime(2026, 5, 30, 7, 0, 0, DateTimeKind.Utc);

        var task = new TaskItem(
            "Clean kitchen",
            _listId,
            _creatorId,
            "Before dinner",
            assigneeId,
            dueDate,
            reminderAtUtc,
            TaskPriority.High);

        task.AssignedToUserId.Should().Be(assigneeId);
        task.DueDate.Should().Be(dueDate);
        task.ReminderAtUtc.Should().Be(reminderAtUtc);
        task.ReminderSentAtUtc.Should().BeNull();
        task.Priority.Should().Be(TaskPriority.High);
    }

    [Fact]
    public void Constructor_WithEmptyAssignee_ThrowsArgumentException()
    {
        var act = () => new TaskItem(
            "Clean kitchen",
            _listId,
            _creatorId,
            assignedToUserId: Guid.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidTitle_ThrowsArgumentException(string? title)
    {
        var act = () => new TaskItem(title!, _listId, _creatorId);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithEmptyListId_ThrowsArgumentException()
    {
        var act = () => new TaskItem("Clean kitchen", Guid.Empty, _creatorId);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithEmptyCreatorId_ThrowsArgumentException()
    {
        var act = () => new TaskItem("Clean kitchen", _listId, Guid.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Update_WithValidData_UpdatesTitleAndNote()
    {
        var task = new TaskItem("Clean kitchen", _listId, _creatorId, "Before dinner");

        task.Update("Mop kitchen", null, Guid.NewGuid(), new DateOnly(2026, 5, 30), null, TaskPriority.Low);

        task.Title.Should().Be("Mop kitchen");
        task.Note.Should().BeNull();
        task.Priority.Should().Be(TaskPriority.Low);
    }

    [Fact]
    public void Update_WhenReminderChanges_ClearsReminderSentTimestamp()
    {
        var task = new TaskItem(
            "Clean kitchen",
            _listId,
            _creatorId,
            reminderAtUtc: new DateTime(2026, 5, 30, 7, 0, 0, DateTimeKind.Utc));
        task.MarkReminderSent(new DateTime(2026, 5, 30, 7, 1, 0, DateTimeKind.Utc));

        task.Update(
            "Clean kitchen",
            null,
            null,
            new DateOnly(2026, 5, 31),
            new DateTime(2026, 5, 31, 7, 0, 0, DateTimeKind.Utc),
            TaskPriority.Normal);

        task.ReminderSentAtUtc.Should().BeNull();
    }

    [Fact]
    public void MarkReminderSent_WithUtcTimestamp_SetsReminderSentAt()
    {
        var task = new TaskItem(
            "Clean kitchen",
            _listId,
            _creatorId,
            reminderAtUtc: new DateTime(2026, 5, 30, 7, 0, 0, DateTimeKind.Utc));
        var sentAt = new DateTime(2026, 5, 30, 7, 1, 0, DateTimeKind.Utc);

        task.MarkReminderSent(sentAt);

        task.ReminderSentAtUtc.Should().Be(sentAt);
    }

    [Fact]
    public void Complete_WithValidUser_MarksCompleted()
    {
        var task = new TaskItem("Clean kitchen", _listId, _creatorId);
        var completerId = Guid.NewGuid();

        task.Complete(completerId);

        task.IsCompleted.Should().BeTrue();
        task.CompletedBy.Should().Be(completerId);
        task.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void Uncomplete_WhenCompleted_ResetsCompletion()
    {
        var task = new TaskItem("Clean kitchen", _listId, _creatorId);
        task.Complete(Guid.NewGuid());

        task.Uncomplete();

        task.IsCompleted.Should().BeFalse();
        task.CompletedBy.Should().BeNull();
        task.CompletedAt.Should().BeNull();
    }
}
