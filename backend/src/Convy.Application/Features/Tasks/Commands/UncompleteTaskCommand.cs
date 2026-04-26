using Convy.Application.Common.Models;
using MediatR;

namespace Convy.Application.Features.Tasks.Commands;

public record UncompleteTaskCommand(Guid ListId, Guid TaskId) : IRequest<Result>;
