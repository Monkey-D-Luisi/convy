using Convy.Application.Common.Models;
using MediatR;

namespace Convy.Application.Features.Tasks.Commands;

public record CompleteTaskCommand(Guid TaskId) : IRequest<Result>;
