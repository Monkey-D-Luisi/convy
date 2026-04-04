using Convy.Application.Common.Models;
using Convy.Application.Features.Invites.DTOs;
using MediatR;

namespace Convy.Application.Features.Invites.Commands;

public record CreateInviteCommand(Guid HouseholdId) : IRequest<Result<InviteDto>>;
