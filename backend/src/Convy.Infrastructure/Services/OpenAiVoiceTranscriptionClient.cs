using OpenAI.Audio;

namespace Convy.Infrastructure.Services;

internal interface IOpenAiVoiceTranscriptionClient
{
    Task<VoiceTranscriptionResult> TranscribeAsync(
        Stream audio,
        string fileName,
        CancellationToken cancellationToken);
}

internal sealed record VoiceTranscriptionResult(
    string Text,
    TimeSpan? Duration,
    string? Language,
    string? Model,
    OpenAiVoiceTokenUsage? Usage);

internal sealed class OpenAiVoiceTranscriptionClient : IOpenAiVoiceTranscriptionClient
{
    private const string TranscriptionPrompt =
        "Shopping list voice note in Spanish or English. Preserve item names, quantities, and units.";

    private readonly AudioClient _audioClient;
    private readonly OpenAiVoiceParsingOptions _options;

    public OpenAiVoiceTranscriptionClient(
        AudioClient audioClient,
        OpenAiVoiceParsingOptions options)
    {
        _audioClient = audioClient;
        _options = options;
    }

    public async Task<VoiceTranscriptionResult> TranscribeAsync(
        Stream audio,
        string fileName,
        CancellationToken cancellationToken)
    {
        var options = new AudioTranscriptionOptions
        {
            ResponseFormat = AudioTranscriptionFormat.Verbose,
            Prompt = TranscriptionPrompt,
        };

        var result = await _audioClient.TranscribeAudioAsync(audio, fileName, options, cancellationToken);
        var transcription = result.Value;

        return new VoiceTranscriptionResult(
            transcription.Text.Trim(),
            transcription.Duration,
            transcription.Language,
            _options.TranscriptionModel,
            MapUsage(transcription.Usage));
    }

    private static OpenAiVoiceTokenUsage? MapUsage(AudioTranscriptionUsage? usage) =>
        usage switch
        {
            AudioTranscriptionTokenUsage tokenUsage => new OpenAiVoiceTokenUsage(
                tokenUsage.InputTokenCount,
                tokenUsage.OutputTokenCount,
                tokenUsage.TotalTokenCount,
                null,
                null,
                tokenUsage.InputTokenDetails?.AudioTokenCount,
                tokenUsage.InputTokenDetails?.TextTokenCount),
            _ => null,
        };
}
