using Convy.Application.Common.Models;
using MediatR;

namespace Convy.Application.Features.Tasks.Commands;

public record DeleteTaskCommand(Guid TaskId) : IRequest<Result>;
