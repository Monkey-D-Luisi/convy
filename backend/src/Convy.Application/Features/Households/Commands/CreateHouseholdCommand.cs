using Convy.Application.Common.Models;
using MediatR;

namespace Convy.Application.Features.Households.Commands;

public record CreateHouseholdCommand(string Name) : IRequest<Result<Guid>>;
