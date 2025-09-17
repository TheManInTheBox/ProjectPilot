using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjectPilot.Core.Interfaces;
using ProjectPilot.Infrastructure.Configuration;

namespace ProjectPilot.Infrastructure.Services;

public class AzureSpeechToTextService : ISpeechToTextService
{
    private readonly AzureSpeechOptions _options;
    private readonly ILogger<AzureSpeechToTextService> _logger;

    public AzureSpeechToTextService(IOptions<AzureSpeechOptions> options, ILogger<AzureSpeechToTextService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> TranscribeAudioAsync(Stream audioStream, string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting transcription for file: {FileName}", fileName);

            var speechConfig = SpeechConfig.FromSubscription(_options.SubscriptionKey, _options.Region);
            speechConfig.SpeechRecognitionLanguage = _options.Language;

            // Copy stream to memory stream for Azure Speech SDK
            using var memoryStream = new MemoryStream();
            await audioStream.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;

            using var audioConfig = AudioConfig.FromStreamInput(
                AudioInputStream.CreatePushStream(AudioStreamFormat.GetWaveFormatPCM(16000, 16, 1)));
            
            using var speechRecognizer = new SpeechRecognizer(speechConfig, audioConfig);

            var result = await speechRecognizer.RecognizeOnceAsync();

            return result.Reason switch
            {
                ResultReason.RecognizedSpeech => result.Text,
                ResultReason.NoMatch => throw new InvalidOperationException("No speech could be recognized from the audio"),
                ResultReason.Canceled => throw new InvalidOperationException($"Speech recognition was cancelled: {result.Text}"),
                _ => throw new InvalidOperationException($"Unknown recognition result: {result.Reason}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transcribing audio file: {FileName}", fileName);
            throw;
        }
    }

    public async Task<string> TranscribeAudioFromUrlAsync(string audioUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting transcription from URL: {AudioUrl}", audioUrl);

            using var httpClient = new HttpClient();
            using var audioStream = await httpClient.GetStreamAsync(audioUrl, cancellationToken);
            
            return await TranscribeAudioAsync(audioStream, audioUrl, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transcribing audio from URL: {AudioUrl}", audioUrl);
            throw;
        }
    }
}