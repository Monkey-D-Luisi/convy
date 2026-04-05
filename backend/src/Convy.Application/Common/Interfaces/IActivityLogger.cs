using Convy.Domain.ValueObjects;

namespace Convy.Application.Common.Interfaces;

public interface IActivityLogger
{
    Task LogAsync(
        Guid householdId,
        ActivityEntityType entityType,
        Guid entityId,
        ActivityActionType actionType,
        Guid performedBy,
        string? metadata = null,
        CancellationToken cancellationToken = default);
}
