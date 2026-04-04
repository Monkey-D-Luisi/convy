using Convy.Application.Common.Models;
using MediatR;

namespace Convy.Application.Features.Invites.Commands;

public record JoinHouseholdCommand(string InviteCode) : IRequest<Result<Guid>>;
