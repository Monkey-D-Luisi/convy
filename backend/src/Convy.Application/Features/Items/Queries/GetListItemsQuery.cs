using Convy.Application.Common.Models;
using Convy.Application.Features.Items.DTOs;
using MediatR;

namespace Convy.Application.Features.Items.Queries;

public record GetListItemsQuery(Guid ListId, bool IncludeCompleted = true) : IRequest<Result<IReadOnlyList<ListItemDto>>>;
