using Convy.Application.Common.Interfaces;
using Convy.Application.Common.Models;
using Convy.Domain.Repositories;
using Convy.Domain.ValueObjects;
using MediatR;

namespace Convy.Application.Features.Items.Commands;

public class ParseVoiceAudioCommandHandler : IRequestHandler<ParseVoiceAudioCommand, Result<VoiceParsingResult>>
{
    private readonly IHouseholdListRepository _listRepository;
    private readonly IHouseholdRepository _householdRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAiVoiceParsingService _voiceParsingService;

    public ParseVoiceAudioCommandHandler(
        IHouseholdListRepository listRepository,
        IHouseholdRepository householdRepository,
        ICurrentUserService currentUser,
        IAiVoiceParsingService voiceParsingService)
    {
        _listRepository = listRepository;
        _householdRepository = householdRepository;
        _currentUser = currentUser;
        _voiceParsingService = voiceParsingService;
    }

    public async Task<Result<VoiceParsingResult>> Handle(ParseVoiceAudioCommand request, CancellationToken cancellationToken)
    {
        var list = await _listRepository.GetByIdAsync(request.ListId, cancellationToken);
        if (list is null)
            return Result<VoiceParsingResult>.Failure(Error.NotFound("List not found."));

        if (list.Type != ListType.Shopping)
            return Result<VoiceParsingResult>.Failure(Error.Validation("Voice item parsing is only supported for shopping lists."));

        var household = await _householdRepository.GetByIdWithMembersAsync(list.HouseholdId, cancellationToken);
        if (household is null || !household.IsMember(_currentUser.UserId))
            return Result<VoiceParsingResult>.Failure(Error.Forbidden("You are not a member of this household."));

        var result = await _voiceParsingService.ParseAudioAsync(
            request.Audio,
            request.FileName,
            household.Id,
            cancellationToken);

        return Result<VoiceParsingResult>.Success(result);
    }
}
