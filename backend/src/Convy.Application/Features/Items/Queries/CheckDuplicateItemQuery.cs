using Convy.Application.Common.Models;
using Convy.Application.Features.Items.DTOs;
using MediatR;

namespace Convy.Application.Features.Items.Queries;

public record CheckDuplicateItemQuery(Guid ListId, string Title) : IRequest<Result<DuplicateCheckDto>>;
