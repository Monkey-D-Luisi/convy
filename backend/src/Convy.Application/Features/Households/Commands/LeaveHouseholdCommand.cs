using Convy.Application.Common.Models;
using MediatR;

namespace Convy.Application.Features.Households.Commands;

public record LeaveHouseholdCommand(Guid HouseholdId) : IRequest<Result>;
