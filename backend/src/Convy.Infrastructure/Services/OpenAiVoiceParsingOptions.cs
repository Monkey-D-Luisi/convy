namespace Convy.Infrastructure.Services;

internal sealed record OpenAiVoiceParsingOptions(
    string TranscriptionModel,
    string ParsingModel,
    int MaxParsedItems = 20,
    int MaxOutputTokenCount = 1200);
