using Convy.Application.Common.Models;
using MediatR;

namespace Convy.Application.Features.Lists.Commands;

public record RenameListCommand(Guid ListId, string NewName) : IRequest<Result>;
