using Convy.Application.Common.Interfaces;
using Convy.Application.Common.Models;
using Convy.Application.Features.Users.DTOs;
using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using MediatR;

namespace Convy.Application.Features.Users.Queries;

public class GetNotificationPreferencesQueryHandler : IRequestHandler<GetNotificationPreferencesQuery, Result<NotificationPreferencesDto>>
{
    private readonly INotificationPreferencesRepository _repository;
    private readonly ICurrentUserService _currentUser;

    public GetNotificationPreferencesQueryHandler(
        INotificationPreferencesRepository repository,
        ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<Result<NotificationPreferencesDto>> Handle(GetNotificationPreferencesQuery request, CancellationToken cancellationToken)
    {
        var preferences = await _repository.GetByUserIdAsync(_currentUser.UserId, cancellationToken)
            ?? NotificationPreferences.CreateDefault(_currentUser.UserId);

        return Result<NotificationPreferencesDto>.Success(Map(preferences));
    }

    private static NotificationPreferencesDto Map(NotificationPreferences preferences) => new(
        preferences.ItemsAdded,
        preferences.TasksAdded,
        preferences.ItemsCompleted,
        preferences.TasksCompleted,
        preferences.ItemTaskChanges,
        preferences.ListChanges,
        preferences.MemberChanges);
}
