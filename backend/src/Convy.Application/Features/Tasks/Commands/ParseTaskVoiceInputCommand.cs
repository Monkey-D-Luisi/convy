using Convy.Application.Common.Models;
using Convy.Domain.ValueObjects;
using MediatR;

namespace Convy.Application.Features.Tasks.Commands;

public record ParseTaskVoiceAudioCommand(
    Guid ListId,
    Stream Audio,
    string FileName,
    string TimeZoneId,
    DateTimeOffset Now,
    long? AudioLengthBytes = null) : IRequest<Result<TaskVoiceParsingResult>>;

public record TaskVoiceParsingResult(
    string Transcription,
    IReadOnlyList<ParsedTaskDto> Tasks);

public record ParsedTaskDto(
    string Title,
    string? Note,
    Guid? AssignedToUserId,
    DateOnly? DueDate,
    DateTime? ReminderAtUtc,
    TaskPriority Priority,
    string? MatchedExistingTask,
    string? AssignedToUserName = null);
