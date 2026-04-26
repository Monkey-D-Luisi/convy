using Convy.Application.Common.Models;
using MediatR;

namespace Convy.Application.Features.Tasks.Commands;

public record UncompleteTaskCommand(Guid TaskId) : IRequest<Result>;
