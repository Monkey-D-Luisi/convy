using Convy.Application.Common.Models;
using Convy.Application.Features.Activity.DTOs;
using MediatR;

namespace Convy.Application.Features.Activity.Queries;

public record GetItemHistoryQuery(Guid ItemId) : IRequest<Result<IReadOnlyList<ActivityLogDto>>>;
