using Convy.Application.Common.Models;
using Convy.Domain.ValueObjects;
using MediatR;

namespace Convy.Application.Features.Tasks.Commands;

public record UpdateTaskCommand(
    Guid ListId,
    Guid TaskId,
    string Title,
    string? Note,
    Guid? AssignedToUserId = null,
    DateOnly? DueDate = null,
    DateTime? ReminderAtUtc = null,
    TaskPriority Priority = TaskPriority.Normal) : IRequest<Result>;
