using Convy.Application.Common.Models;
using Convy.Domain.ValueObjects;
using MediatR;

namespace Convy.Application.Features.Lists.Commands;

public record CreateListCommand(Guid HouseholdId, string Name, ListType Type) : IRequest<Result<Guid>>;
