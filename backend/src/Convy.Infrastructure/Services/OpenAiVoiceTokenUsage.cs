namespace Convy.Infrastructure.Services;

internal sealed record OpenAiVoiceTokenUsage(
    int? InputTokenCount,
    int? OutputTokenCount,
    int? TotalTokenCount,
    int? CachedTokenCount,
    int? ReasoningTokenCount,
    int? AudioTokenCount,
    int? TextTokenCount);
