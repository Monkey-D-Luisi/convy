using System.Reflection;
using Convy.Application.Common.Interfaces;
using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using Convy.Domain.ValueObjects;
using Convy.Infrastructure.Persistence;
using Convy.Infrastructure.Repositories;
using Convy.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Convy.Infrastructure.Tests.Services;

public class RecurringItemServiceTests
{
    [Fact]
    public async Task ProcessDueItemsAsync_KeepsSingleActiveRecurringItemAcrossCycles()
    {
        var databaseName = Guid.NewGuid().ToString();
        var services = CreateServices(databaseName);
        var userId = Guid.NewGuid();
        var householdId = Guid.NewGuid();
        var list = new HouseholdList("Shopping", ListType.Shopping, householdId, userId);
        var originalItem = new ListItem("Milk", list.Id, userId);
        originalItem.SetRecurrence(RecurrenceFrequency.Daily, 1);
        originalItem.Complete(userId);
        SetDate(originalItem, nameof(ListItem.NextDueDate), DateTime.UtcNow.AddMinutes(-1));

        await using (var scope = services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ConvyDbContext>();
            context.HouseholdLists.Add(list);
            context.ListItems.Add(originalItem);
            await context.SaveChangesAsync();
        }

        await ProcessDueItemsAsync(services);

        await using (var scope = services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ConvyDbContext>();
            var cycleItems = await context.ListItems.ToListAsync();
            var generatedItem = cycleItems.Single(i => !i.IsCompleted);
            generatedItem.Complete(userId);

            foreach (var item in cycleItems.Where(i => i.RecurrenceFrequency != null && i.IsCompleted))
            {
                SetDate(item, nameof(ListItem.NextDueDate), DateTime.UtcNow.AddMinutes(-1));
            }

            await context.SaveChangesAsync();
        }

        await ProcessDueItemsAsync(services);

        await using (var scope = services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ConvyDbContext>();
            var items = await context.ListItems.ToListAsync();

            items.Should().HaveCount(3);
            items.Where(i => !i.IsCompleted).Should().ContainSingle(i => i.Title == "Milk");
            items.Where(i => i.RecurrenceFrequency != null).Should().ContainSingle(i => !i.IsCompleted);
        }
    }

    private static ServiceProvider CreateServices(string databaseName)
    {
        var services = new ServiceCollection();
        services.AddDbContext<ConvyDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddScoped<IListItemRepository, ListItemRepository>();
        services.AddScoped<IHouseholdListRepository, HouseholdListRepository>();
        services.AddScoped(_ => Substitute.For<IActivityLogger>());

        return services.BuildServiceProvider();
    }

    private static async Task ProcessDueItemsAsync(ServiceProvider services)
    {
        var service = new RecurringItemService(services.GetRequiredService<IServiceScopeFactory>(), NullLogger<RecurringItemService>.Instance);
        var method = typeof(RecurringItemService).GetMethod("ProcessDueItemsAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var task = (Task)method.Invoke(service, [CancellationToken.None])!;

        await task;
    }

    private static void SetDate<T>(T entity, string propertyName, DateTime value) where T : class =>
        typeof(T).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)!.SetValue(entity, value);
}
