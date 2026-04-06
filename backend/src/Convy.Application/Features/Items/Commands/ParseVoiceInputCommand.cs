using Convy.Application.Common.Models;
using MediatR;

namespace Convy.Application.Features.Items.Commands;

public record ParseVoiceInputCommand(Guid ListId, string TranscribedText) : IRequest<Result<List<ParsedItemDto>>>;

public record ParsedItemDto(string Title, int? Quantity, string? Unit);
