using Convy.Application.Common.Interfaces;
using Convy.Domain.Entities;
using Convy.Domain.ValueObjects;
using Convy.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;

namespace Convy.Infrastructure.Services;

public class AiUsageRecorder : IAiUsageRecorder
{
    private readonly ConvyDbContext _context;
    private readonly IConfiguration _configuration;

    public AiUsageRecorder(ConvyDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task RecordAsync(AiUsageRecordRequest request, CancellationToken cancellationToken = default)
    {
        var status = request.Status.Equals("success", StringComparison.OrdinalIgnoreCase)
            ? AiUsageStatus.Success
            : AiUsageStatus.Failure;

        var usageEvent = new AiUsageEvent(
            request.HouseholdId,
            request.Feature,
            request.Operation,
            request.Model,
            status,
            request.LatencyMs,
            request.InputTokens,
            request.OutputTokens,
            request.CachedTokens,
            request.ReasoningTokens,
            request.AudioTokens,
            request.TextTokens,
            request.AudioDurationSeconds,
            EstimateMicros(request),
            request.ErrorType);

        await _context.AiUsageEvents.AddAsync(usageEvent, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private long? EstimateMicros(AiUsageRecordRequest request) =>
        (request.Operation.Equals("transcription", StringComparison.OrdinalIgnoreCase) ||
         request.Operation.Equals("task_transcription", StringComparison.OrdinalIgnoreCase))
            ? EstimateTranscriptionMicros(request)
            : EstimateParsingMicros(request);

    private long? EstimateTranscriptionMicros(AiUsageRecordRequest request)
    {
        if (request.AudioDurationSeconds is null or < 0)
            return null;

        var price = GetPrice("TranscriptionAudioInputMicrosPerSecond");
        return price is null
            ? null
            : (long)Math.Round((decimal)request.AudioDurationSeconds.Value * price.Value, MidpointRounding.AwayFromZero);
    }

    private long? EstimateParsingMicros(AiUsageRecordRequest request) =>
        OpenAiParsingCost.Estimate(_configuration, request.InputTokens, request.OutputTokens,
            request.CachedTokens, request.CacheWriteTokens);

    private decimal? GetPrice(string key)
    {
        var value = _configuration[$"OpenAI:Costs:{key}"];
        return decimal.TryParse(value, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var parsed)
            && parsed >= 0 ? parsed
            : null;
    }
}
