using Convy.Application.Common.Models;
using Convy.Domain.ValueObjects;
using MediatR;

namespace Convy.Application.Features.Items.Commands;

public record UpdateItemCommand(
    Guid ItemId, string Title, int? Quantity, string? Unit, string? Note,
    RecurrenceFrequency? RecurrenceFrequency, int? RecurrenceInterval) : IRequest<Result>;
