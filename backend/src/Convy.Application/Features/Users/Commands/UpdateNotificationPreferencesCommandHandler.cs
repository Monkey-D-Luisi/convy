using Convy.Application.Common.Interfaces;
using Convy.Application.Common.Models;
using Convy.Application.Features.Users.DTOs;
using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using MediatR;

namespace Convy.Application.Features.Users.Commands;

public class UpdateNotificationPreferencesCommandHandler : IRequestHandler<UpdateNotificationPreferencesCommand, Result<NotificationPreferencesDto>>
{
    private readonly INotificationPreferencesRepository _repository;
    private readonly ICurrentUserService _currentUser;

    public UpdateNotificationPreferencesCommandHandler(
        INotificationPreferencesRepository repository,
        ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<Result<NotificationPreferencesDto>> Handle(UpdateNotificationPreferencesCommand request, CancellationToken cancellationToken)
    {
        var preferences = await _repository.GetByUserIdAsync(_currentUser.UserId, cancellationToken);
        if (preferences is null)
        {
            preferences = NotificationPreferences.CreateDefault(_currentUser.UserId);
            await _repository.AddAsync(preferences, cancellationToken);
        }

        preferences.Update(
            request.ItemsAdded,
            request.TasksAdded,
            request.ItemsCompleted,
            request.TasksCompleted,
            request.ItemTaskChanges,
            request.ListChanges,
            request.MemberChanges);

        await _repository.SaveChangesAsync(cancellationToken);

        return Result<NotificationPreferencesDto>.Success(new NotificationPreferencesDto(
            preferences.ItemsAdded,
            preferences.TasksAdded,
            preferences.ItemsCompleted,
            preferences.TasksCompleted,
            preferences.ItemTaskChanges,
            preferences.ListChanges,
            preferences.MemberChanges));
    }
}
