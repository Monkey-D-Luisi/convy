using Convy.Application.Common.Models;
using MediatR;

namespace Convy.Application.Features.Items.Commands;

public record CreateItemCommand(Guid ListId, string Title, int? Quantity, string? Unit, string? Note) : IRequest<Result<Guid>>;
