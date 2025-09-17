using Microsoft.Extensions.Logging;
using ProjectPilot.Application.DTOs;
using ProjectPilot.Core.Interfaces;
using ProjectPilot.Core.Models;

namespace ProjectPilot.Application.Services;

public class TranscriptionOrchestrationService : ITranscriptionService
{
    private readonly ISpeechToTextService _speechToTextService;
    private readonly IOpenAIService _openAIService;
    private readonly ILogger<TranscriptionOrchestrationService> _logger;
    private readonly Dictionary<string, MeetingTranscription> _transcriptionStore = new();

    public TranscriptionOrchestrationService(
        ISpeechToTextService speechToTextService,
        IOpenAIService openAIService,
        ILogger<TranscriptionOrchestrationService> logger)
    {
        _speechToTextService = speechToTextService;
        _openAIService = openAIService;
        _logger = logger;
    }

    public async Task<MeetingTranscription> StartTranscriptionAsync(string audioFileName, Stream audioStream, string title = "", CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting transcription process for file: {FileName}", audioFileName);

            var transcription = new MeetingTranscription
            {
                Title = string.IsNullOrEmpty(title) ? Path.GetFileNameWithoutExtension(audioFileName) : title,
                AudioFileName = audioFileName,
                StartTime = DateTime.UtcNow,
                Status = TranscriptionStatus.InProgress
            };

            // Store the transcription record
            _transcriptionStore[transcription.Id] = transcription;

            // Process in background (in a real implementation, this would be queued)
            _ = ProcessTranscriptionAsync(transcription, audioStream, cancellationToken);

            _logger.LogInformation("Transcription process started with ID: {TranscriptionId}", transcription.Id);
            return transcription;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting transcription for file: {FileName}", audioFileName);
            throw;
        }
    }

    public async Task<MeetingTranscription> GetTranscriptionAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!_transcriptionStore.TryGetValue(id, out var transcription))
        {
            throw new KeyNotFoundException($"Transcription with ID {id} not found");
        }

        return transcription;
    }

    public async Task<List<MeetingTranscription>> GetTranscriptionsAsync(int skip = 0, int take = 20, CancellationToken cancellationToken = default)
    {
        return _transcriptionStore.Values
            .OrderByDescending(t => t.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToList();
    }

    public async Task<MeetingTranscription> UpdateTranscriptionAsync(MeetingTranscription transcription, CancellationToken cancellationToken = default)
    {
        if (!_transcriptionStore.ContainsKey(transcription.Id))
        {
            throw new KeyNotFoundException($"Transcription with ID {transcription.Id} not found");
        }

        transcription.UpdatedAt = DateTime.UtcNow;
        _transcriptionStore[transcription.Id] = transcription;
        return transcription;
    }

    public async Task DeleteTranscriptionAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!_transcriptionStore.Remove(id))
        {
            throw new KeyNotFoundException($"Transcription with ID {id} not found");
        }
    }

    private async Task ProcessTranscriptionAsync(MeetingTranscription transcription, Stream audioStream, CancellationToken cancellationToken)
    {
        try
        {
            // Update status
            transcription.Status = TranscriptionStatus.Transcribing;
            transcription.UpdatedAt = DateTime.UtcNow;

            // Perform speech-to-text
            var transcriptionText = await _speechToTextService.TranscribeAudioAsync(audioStream, transcription.AudioFileName, cancellationToken);
            transcription.TranscriptionText = transcriptionText;
            transcription.UpdatedAt = DateTime.UtcNow;

            // Update status
            transcription.Status = TranscriptionStatus.Summarizing;
            transcription.UpdatedAt = DateTime.UtcNow;

            // Generate summary
            var summary = await _openAIService.SummarizeMeetingAsync(transcriptionText, cancellationToken);
            transcription.Summary = summary;
            transcription.UpdatedAt = DateTime.UtcNow;

            // Update status
            transcription.Status = TranscriptionStatus.ExtractingTasks;
            transcription.UpdatedAt = DateTime.UtcNow;

            // Extract tasks
            var tasks = await _openAIService.ExtractTasksAsync(transcriptionText, summary, cancellationToken);
            transcription.ExtractedTasks = tasks;
            
            // Mark as completed
            transcription.Status = TranscriptionStatus.Completed;
            transcription.EndTime = DateTime.UtcNow;
            transcription.UpdatedAt = DateTime.UtcNow;

            _logger.LogInformation("Transcription completed successfully for ID: {TranscriptionId}", transcription.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing transcription for ID: {TranscriptionId}", transcription.Id);
            transcription.Status = TranscriptionStatus.Failed;
            transcription.UpdatedAt = DateTime.UtcNow;
        }
    }
}