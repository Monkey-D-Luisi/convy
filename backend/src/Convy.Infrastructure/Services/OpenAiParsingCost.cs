using Microsoft.Extensions.Configuration;

namespace Convy.Infrastructure.Services;

internal static class OpenAiParsingCost
{
    internal static long? Estimate(IConfiguration configuration, int? input, int? output, int? cached, int? cacheWrite) =>
        Calculate(configuration, input, output, cached, cacheWrite) is { } cost
            ? (long)Math.Round(cost, MidpointRounding.AwayFromZero) : null;

    internal static decimal? Calculate(IConfiguration configuration, int? input, int? output, int? cached, int? cacheWrite)
    {
        // Cached reads and cache writes are subsets of input. Reasoning is already part of output.
        if (input is < 0 || output is < 0 || cached is < 0 || cacheWrite is < 0 ||
            (long)(cached ?? 0) + (cacheWrite ?? 0) > (input ?? 0)) return null;
        if (input is null && output is null) return null;
        decimal total = 0;
        foreach (var (tokens, key) in new[] {
            ((input ?? 0) - (cached ?? 0) - (cacheWrite ?? 0), "ParsingInputMicrosPer1KTokens"),
            (cached ?? 0, "ParsingCachedInputMicrosPer1KTokens"),
            (cacheWrite ?? 0, "ParsingCacheWriteMicrosPer1KTokens"),
            (output ?? 0, "ParsingOutputMicrosPer1KTokens") })
        {
            if (tokens == 0) continue;
            var price = Price(configuration, key);
            if (price is null) return null;
            total += tokens / 1000m * price.Value;
        }
        return total;
    }

    internal static decimal? Price(IConfiguration configuration, string key) =>
        decimal.TryParse(configuration[$"OpenAI:Costs:{key}"], System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture, out var value) && value >= 0 ? value : null;
}
