using Convy.Application.Common.Models;
using Convy.Application.Features.Tasks.DTOs;
using MediatR;

namespace Convy.Application.Features.Tasks.Queries;

public record GetListTasksQuery(
    Guid ListId,
    string? Status = null,
    Guid? CreatedBy = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null
) : IRequest<Result<IReadOnlyList<TaskItemDto>>>;
