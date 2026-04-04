using Convy.Application.Common.Models;
using Convy.Application.Features.Items.DTOs;
using MediatR;

namespace Convy.Application.Features.Items.Queries;

public record GetItemSuggestionsQuery(Guid HouseholdId, string? Query = null) : IRequest<Result<ItemSuggestionsDto>>;
