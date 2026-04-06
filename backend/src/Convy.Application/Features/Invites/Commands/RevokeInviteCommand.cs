using Convy.Application.Common.Models;
using MediatR;

namespace Convy.Application.Features.Invites.Commands;

public record RevokeInviteCommand(Guid InviteId) : IRequest<Result>;
