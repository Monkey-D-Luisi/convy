using Convy.Application.Common.Models;
using MediatR;

namespace Convy.Application.Features.Tasks.Commands;

public record UpdateTaskCommand(Guid ListId, Guid TaskId, string Title, string? Note) : IRequest<Result>;
