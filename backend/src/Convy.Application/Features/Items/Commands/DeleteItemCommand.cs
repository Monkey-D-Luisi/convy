using Convy.Application.Common.Models;
using MediatR;

namespace Convy.Application.Features.Items.Commands;

public record DeleteItemCommand(Guid ItemId) : IRequest<Result>;
