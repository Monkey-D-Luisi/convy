using Convy.Application.Common.Models;
using Convy.Application.Features.Lists.DTOs;
using MediatR;

namespace Convy.Application.Features.Lists.Queries;

public record GetHouseholdListsQuery(Guid HouseholdId, bool IncludeArchived = false) : IRequest<Result<IReadOnlyList<HouseholdListDto>>>;
