using Convy.Application.Common.Interfaces;
using Convy.Application.Common.Models;
using MediatR;

namespace Convy.Application.Features.Items.Commands;

public record ParseVoiceAudioCommand(Guid ListId, Stream Audio, string FileName) : IRequest<Result<VoiceParsingResult>>;

public record ParsedItemDto(string Title, int? Quantity, string? Unit, string? MatchedExistingItem);
