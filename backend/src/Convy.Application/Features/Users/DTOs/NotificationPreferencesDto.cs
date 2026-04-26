namespace Convy.Application.Features.Users.DTOs;

public record NotificationPreferencesDto(
    bool ItemsAdded,
    bool TasksAdded,
    bool ItemsCompleted,
    bool TasksCompleted,
    bool ItemTaskChanges,
    bool ListChanges,
    bool MemberChanges);
