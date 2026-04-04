using Convy.Application.Common.Models;
using Convy.Application.Features.Households.DTOs;
using MediatR;

namespace Convy.Application.Features.Households.Queries;

public record GetHouseholdQuery(Guid HouseholdId) : IRequest<Result<HouseholdDetailDto>>;
