using Convy.Application.Common.Models;
using Convy.Domain.ValueObjects;
using MediatR;

namespace Convy.Application.Features.Items.Commands;

public record CreateItemCommand(
    Guid ListId, string Title, int? Quantity, string? Unit, string? Note,
    RecurrenceFrequency? RecurrenceFrequency, int? RecurrenceInterval) : IRequest<Result<Guid>>;
