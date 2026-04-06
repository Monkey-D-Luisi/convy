using Convy.Application.Common.Models;
using MediatR;

namespace Convy.Application.Features.Households.Commands;

public record RenameHouseholdCommand(Guid HouseholdId, string NewName) : IRequest<Result>;
