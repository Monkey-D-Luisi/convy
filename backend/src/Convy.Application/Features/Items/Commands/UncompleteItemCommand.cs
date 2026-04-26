using Convy.Application.Common.Models;
using MediatR;

namespace Convy.Application.Features.Items.Commands;

public record UncompleteItemCommand(Guid ListId, Guid ItemId) : IRequest<Result>;
