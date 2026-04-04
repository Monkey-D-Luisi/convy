using Convy.Application.Common.Models;
using MediatR;

namespace Convy.Application.Features.Items.Commands;

public record UpdateItemCommand(Guid ItemId, string Title, int? Quantity, string? Unit, string? Note) : IRequest<Result>;
