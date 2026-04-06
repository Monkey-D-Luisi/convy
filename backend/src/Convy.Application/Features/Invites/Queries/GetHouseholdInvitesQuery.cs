using Convy.Application.Common.Models;
using Convy.Application.Features.Invites.DTOs;
using MediatR;

namespace Convy.Application.Features.Invites.Queries;

public record GetHouseholdInvitesQuery(Guid HouseholdId) : IRequest<Result<List<InviteDto>>>;
