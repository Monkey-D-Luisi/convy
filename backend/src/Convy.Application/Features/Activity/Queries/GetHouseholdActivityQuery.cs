using Convy.Application.Common.Models;
using Convy.Application.Features.Activity.DTOs;
using MediatR;

namespace Convy.Application.Features.Activity.Queries;

public record GetHouseholdActivityQuery(Guid HouseholdId, int Limit = 50) : IRequest<Result<IReadOnlyList<ActivityLogDto>>>;
