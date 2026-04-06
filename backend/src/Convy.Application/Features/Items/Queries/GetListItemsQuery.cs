using Convy.Application.Common.Models;
using Convy.Application.Features.Items.DTOs;
using MediatR;

namespace Convy.Application.Features.Items.Queries;

public record GetListItemsQuery(
    Guid ListId,
    string? Status = null,
    Guid? CreatedBy = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null
) : IRequest<Result<IReadOnlyList<ListItemDto>>>;
