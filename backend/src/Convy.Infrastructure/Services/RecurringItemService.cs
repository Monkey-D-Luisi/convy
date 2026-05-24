using Convy.Domain.Entities;
using Convy.Domain.ValueObjects;
using Convy.Application.Common.Interfaces;
using Convy.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Convy.Infrastructure.Services;

public class RecurringItemService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RecurringItemService> _logger;
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(15);

    public RecurringItemService(IServiceScopeFactory scopeFactory, ILogger<RecurringItemService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDueItemsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing recurring items");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async Task ProcessDueItemsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var itemRepository = scope.ServiceProvider.GetRequiredService<IListItemRepository>();
        var listRepository = scope.ServiceProvider.GetRequiredService<IHouseholdListRepository>();
        var activityLogger = scope.ServiceProvider.GetRequiredService<IActivityLogger>();

        var dueItems = await itemRepository.GetDueRecurringItemsAsync(DateTime.UtcNow, cancellationToken);

        foreach (var item in dueItems)
        {
            if (!item.IsCompleted) continue;

            var newItem = new ListItem(
                item.Title,
                item.ListId,
                item.CreatedBy,
                item.Quantity,
                item.Unit,
                item.Note);
            item.TransferRecurrenceTo(newItem);

            await itemRepository.AddAsync(newItem, cancellationToken);

            var list = await listRepository.GetByIdAsync(newItem.ListId, cancellationToken);
            if (list is not null)
            {
                await activityLogger.LogAsync(
                    list.HouseholdId,
                    ActivityEntityType.Item,
                    newItem.Id,
                    ActivityActionType.Created,
                    newItem.CreatedBy,
                    newItem.Title,
                    cancellationToken);
            }

            _logger.LogInformation("Created recurring item '{Title}' for list {ListId}", item.Title, item.ListId);
        }

        await itemRepository.SaveChangesAsync(cancellationToken);
    }
}
