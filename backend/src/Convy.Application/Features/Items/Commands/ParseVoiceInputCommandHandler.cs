using System.Diagnostics;
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
    private readonly IVoiceParseEventRepository _voiceParseEventRepository;
    private readonly IOpenAiVoiceCostEstimator _costEstimator;
    private readonly ICurrentUserService _currentUser;
    private readonly IAiVoiceParsingService _voiceParsingService;

    public ParseVoiceAudioCommandHandler(
        IHouseholdListRepository listRepository,
        IHouseholdRepository householdRepository,
        IVoiceParseEventRepository voiceParseEventRepository,
        IOpenAiVoiceCostEstimator costEstimator,
        ICurrentUserService currentUser,
        IAiVoiceParsingService voiceParsingService)
    {
        _listRepository = listRepository;
        _householdRepository = householdRepository;
        _voiceParseEventRepository = voiceParseEventRepository;
        _costEstimator = costEstimator;
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

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await _voiceParsingService.ParseAudioAsync(
                request.Audio,
                request.FileName,
                household.Id,
                cancellationToken);

            stopwatch.Stop();
            if (result.Telemetry is not null)
            {
                await RecordVoiceParseEventAsync(
                    household.Id,
                    request.AudioLengthBytes,
                    result.Telemetry,
                    cancellationToken);
            }

            return Result<VoiceParsingResult>.Success(result);
        }
        catch
        {
            stopwatch.Stop();
            var telemetry = new VoiceParsingTelemetry(
                VoiceParseStatus.ProviderError,
                AudioDurationSeconds: null,
                ParsedItemsCount: 0,
                InputTokens: null,
                OutputTokens: null,
                CachedTokens: null,
                ReasoningTokens: null,
                LatencyMs: stopwatch.ElapsedMilliseconds);
            await RecordVoiceParseEventAsync(household.Id, request.AudioLengthBytes, telemetry, cancellationToken);
            throw;
        }
    }

    private async Task RecordVoiceParseEventAsync(
        Guid householdId,
        long? audioLengthBytes,
        VoiceParsingTelemetry telemetry,
        CancellationToken cancellationToken)
    {
        var estimatedCostMicros = _costEstimator.EstimateMicros(telemetry);
        var voiceParseEvent = new Domain.Entities.VoiceParseEvent(
            _currentUser.UserId,
            householdId,
            telemetry.Status,
            audioLengthBytes,
            telemetry.AudioDurationSeconds,
            telemetry.ParsedItemsCount,
            telemetry.InputTokens,
            telemetry.OutputTokens,
            telemetry.CachedTokens,
            telemetry.ReasoningTokens,
            estimatedCostMicros,
            telemetry.LatencyMs);

        await _voiceParseEventRepository.AddAsync(voiceParseEvent, cancellationToken);
        await _voiceParseEventRepository.SaveChangesAsync(cancellationToken);
    }
}
