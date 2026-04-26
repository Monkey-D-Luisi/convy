using Convy.Application.Common.Models;
using Convy.Application.Features.Users.DTOs;
using MediatR;

namespace Convy.Application.Features.Users.Commands;

public record UpdateNotificationPreferencesCommand(
    bool ItemsAdded,
    bool TasksAdded,
    bool ItemsCompleted,
    bool TasksCompleted,
    bool ItemTaskChanges,
    bool ListChanges,
    bool MemberChanges) : IRequest<Result<NotificationPreferencesDto>>;
