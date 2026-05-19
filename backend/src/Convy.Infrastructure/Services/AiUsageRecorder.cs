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
        request.Operation.Equals("transcription", StringComparison.OrdinalIgnoreCase)
            ? EstimateTranscriptionMicros(request)
            : EstimateParsingMicros(request);

    private long? EstimateTranscriptionMicros(AiUsageRecordRequest request)
    {
        if (request.AudioDurationSeconds is null)
            return null;

        var price = GetPrice("TranscriptionAudioInputMicrosPerSecond");
        return price is null
            ? null
            : (long)Math.Round((decimal)request.AudioDurationSeconds.Value * price.Value, MidpointRounding.AwayFromZero);
    }

    private long? EstimateParsingMicros(AiUsageRecordRequest request)
    {
        decimal total = 0;
        var hasCostableUsage = false;
        var cachedTokens = request.CachedTokens ?? 0;
        var inputTokens = request.InputTokens ?? 0;
        var nonCachedInputTokens = Math.Max(0, inputTokens - cachedTokens);

        if (nonCachedInputTokens > 0)
        {
            var price = GetPrice("ParsingInputMicrosPer1KTokens");
            if (price is null)
                return null;

            total += nonCachedInputTokens / 1000m * price.Value;
            hasCostableUsage = true;
        }

        if (cachedTokens > 0)
        {
            var price = GetPrice("ParsingCachedInputMicrosPer1KTokens");
            if (price is null)
                return null;

            total += cachedTokens / 1000m * price.Value;
            hasCostableUsage = true;
        }

        if (request.OutputTokens is > 0)
        {
            var price = GetPrice("ParsingOutputMicrosPer1KTokens");
            if (price is null)
                return null;

            total += request.OutputTokens.Value / 1000m * price.Value;
            hasCostableUsage = true;
        }

        if (request.ReasoningTokens is > 0)
        {
            var price = GetPrice("ParsingReasoningMicrosPer1KTokens");
            if (price is null)
                return null;

            total += request.ReasoningTokens.Value / 1000m * price.Value;
            hasCostableUsage = true;
        }

        return hasCostableUsage ? (long)Math.Round(total, MidpointRounding.AwayFromZero) : null;
    }

    private decimal? GetPrice(string key)
    {
        var value = _configuration[$"OpenAI:Costs:{key}"];
        return decimal.TryParse(value, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }
}
