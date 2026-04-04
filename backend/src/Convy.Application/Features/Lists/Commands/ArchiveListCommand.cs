using Convy.Application.Common.Models;
using MediatR;

namespace Convy.Application.Features.Lists.Commands;

public record ArchiveListCommand(Guid ListId) : IRequest<Result>;
