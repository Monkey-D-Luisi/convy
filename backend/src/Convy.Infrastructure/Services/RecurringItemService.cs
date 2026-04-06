using Convy.Domain.Entities;
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
            newItem.SetRecurrence(item.RecurrenceFrequency!.Value, item.RecurrenceInterval!.Value);

            await itemRepository.AddAsync(newItem, cancellationToken);
            item.AdvanceRecurrence();

            _logger.LogInformation("Created recurring item '{Title}' for list {ListId}", item.Title, item.ListId);
        }

        await itemRepository.SaveChangesAsync(cancellationToken);
    }
}
