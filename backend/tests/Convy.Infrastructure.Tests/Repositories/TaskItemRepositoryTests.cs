using Convy.Domain.Entities;
using Convy.Infrastructure.Persistence;
using Convy.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Convy.Infrastructure.Tests.Repositories;

public class TaskItemRepositoryTests
{
    [Fact]
    public async Task GetDueRemindersAsync_AppliesLimitInReminderOrder()
    {
        var options = new DbContextOptionsBuilder<ConvyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new ConvyDbContext(options);
        var listId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var first = new TaskItem("First", listId, creatorId, reminderAtUtc: DateTime.UtcNow.AddMinutes(-10));
        var second = new TaskItem("Second", listId, creatorId, reminderAtUtc: DateTime.UtcNow.AddMinutes(-5));
        var third = new TaskItem("Third", listId, creatorId, reminderAtUtc: DateTime.UtcNow.AddMinutes(-1));
        context.TaskItems.AddRange(third, second, first);
        await context.SaveChangesAsync();
        var repository = new TaskItemRepository(context);

        var result = await repository.GetDueRemindersAsync(DateTime.UtcNow, limit: 2);

        result.Select(task => task.Title).Should().Equal("First", "Second");
    }
}
