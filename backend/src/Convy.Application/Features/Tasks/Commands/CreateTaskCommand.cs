using Convy.Application.Common.Models;
using MediatR;

namespace Convy.Application.Features.Tasks.Commands;

public record CreateTaskCommand(Guid ListId, string Title, string? Note) : IRequest<Result<Guid>>;
