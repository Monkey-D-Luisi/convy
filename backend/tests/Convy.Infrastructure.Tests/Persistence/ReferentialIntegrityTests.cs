using Convy.Domain.Entities;
using Convy.Domain.ValueObjects;
using Convy.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Convy.Infrastructure.Tests.Persistence;

public class ReferentialIntegrityTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("convy_fk_tests")
        .WithUsername("convy")
        .WithPassword("test_password")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task HouseholdLists_RejectUnknownHouseholdId()
    {
        await using var context = CreateContext();
        var owner = new User("firebase-owner", "Owner", "owner@example.com");
        context.Users.Add(owner);
        await context.SaveChangesAsync();
        context.HouseholdLists.Add(new HouseholdList("Orphan", ListType.Shopping, Guid.NewGuid(), owner.Id));

        var act = () => context.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task DeletingList_CascadesItemsAndTasks()
    {
        await using var context = CreateContext();
        var owner = new User("firebase-owner-cascade", "Owner", "owner-cascade@example.com");
        var household = new Household("Home", owner.Id);
        var list = new HouseholdList("Shared", ListType.Tasks, household.Id, owner.Id);
        context.AddRange(owner, household, list);
        context.ListItems.Add(new ListItem("Milk", list.Id, owner.Id));
        context.TaskItems.Add(new TaskItem("Clean kitchen", list.Id, owner.Id));
        await context.SaveChangesAsync();

        context.HouseholdLists.Remove(list);
        await context.SaveChangesAsync();

        (await context.ListItems.CountAsync()).Should().Be(0);
        (await context.TaskItems.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task DeletingUser_CascadesDeviceTokensAndNotificationPreferences()
    {
        await using var context = CreateContext();
        var user = new User("firebase-token-user", "Token User", "token-user@example.com");
        context.Users.Add(user);
        context.DeviceTokens.Add(new DeviceToken(user.Id, "device-token", "Android"));
        context.NotificationPreferences.Add(NotificationPreferences.CreateDefault(user.Id));
        await context.SaveChangesAsync();

        context.Users.Remove(user);
        await context.SaveChangesAsync();

        (await context.DeviceTokens.CountAsync()).Should().Be(0);
        (await context.NotificationPreferences.CountAsync()).Should().Be(0);
    }

    private ConvyDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ConvyDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;
        return new ConvyDbContext(options);
    }
}
