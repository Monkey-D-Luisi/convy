namespace Convy.Domain.ValueObjects;

public enum VoiceParseStatus
{
    Success = 0,
    EmptyTranscription = 1,
    ParseEmpty = 2,
    ProviderError = 3,
}
