using Convy.Application.Common.Interfaces;
using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using Convy.Domain.ValueObjects;

namespace Convy.Infrastructure.Services;

public class ActivityLogger : IActivityLogger
{
    private readonly IActivityLogRepository _repository;

    public ActivityLogger(IActivityLogRepository repository)
    {
        _repository = repository;
    }

    public async Task LogAsync(
        Guid householdId,
        ActivityEntityType entityType,
        Guid entityId,
        ActivityActionType actionType,
        Guid performedBy,
        string? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var log = new ActivityLog(householdId, entityType, entityId, actionType, performedBy, metadata);

        await _repository.AddAsync(log, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
    }
}
