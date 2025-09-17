using Microsoft.Extensions.Logging;
using ProjectPilot.Application.DTOs;
using ProjectPilot.Core.Interfaces;
using ProjectPilot.Core.Models;

namespace ProjectPilot.Application.Services;

public class TranscriptionOrchestrationService : ITranscriptionService
{
    private readonly ISpeechToTextService _speechToTextService;
    private readonly IOpenAIService _openAIService;
    private readonly IRepository<MeetingTranscription> _transcriptionRepository;
    private readonly ILogger<TranscriptionOrchestrationService> _logger;

    public TranscriptionOrchestrationService(
        ISpeechToTextService speechToTextService,
        IOpenAIService openAIService,
        IRepository<MeetingTranscription> transcriptionRepository,
        ILogger<TranscriptionOrchestrationService> logger)
    {
        _speechToTextService = speechToTextService;
        _openAIService = openAIService;
        _transcriptionRepository = transcriptionRepository;
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

            // Save to repository
            transcription = await _transcriptionRepository.AddAsync(transcription, cancellationToken);

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
        try
        {
            return await _transcriptionRepository.GetByIdAsync(id, cancellationToken);
        }
        catch (KeyNotFoundException)
        {
            throw new KeyNotFoundException($"Transcription with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving transcription {TranscriptionId}", id);
            throw;
        }
    }

    public async Task<List<MeetingTranscription>> GetTranscriptionsAsync(int skip = 0, int take = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _transcriptionRepository.GetAllAsync(skip, take, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving transcriptions");
            throw;
        }
    }

    public async Task<MeetingTranscription> UpdateTranscriptionAsync(MeetingTranscription transcription, CancellationToken cancellationToken = default)
    {
        try
        {
            transcription.UpdatedAt = DateTime.UtcNow;
            return await _transcriptionRepository.UpdateAsync(transcription, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating transcription {TranscriptionId}", transcription.Id);
            throw;
        }
    }

    public async Task DeleteTranscriptionAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            await _transcriptionRepository.DeleteAsync(id, cancellationToken);
            _logger.LogInformation("Transcription deleted: {TranscriptionId}", id);
        }
        catch (KeyNotFoundException)
        {
            throw new KeyNotFoundException($"Transcription with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting transcription {TranscriptionId}", id);
            throw;
        }
    }

    private async Task ProcessTranscriptionAsync(MeetingTranscription transcription, Stream audioStream, CancellationToken cancellationToken)
    {
        try
        {
            // Update status
            transcription.Status = TranscriptionStatus.Transcribing;
            transcription.UpdatedAt = DateTime.UtcNow;
            await _transcriptionRepository.UpdateAsync(transcription, cancellationToken);

            // Perform speech-to-text
            var transcriptionText = await _speechToTextService.TranscribeAudioAsync(audioStream, transcription.AudioFileName, cancellationToken);
            transcription.TranscriptionText = transcriptionText;
            transcription.UpdatedAt = DateTime.UtcNow;
            await _transcriptionRepository.UpdateAsync(transcription, cancellationToken);

            // Update status
            transcription.Status = TranscriptionStatus.Summarizing;
            transcription.UpdatedAt = DateTime.UtcNow;
            await _transcriptionRepository.UpdateAsync(transcription, cancellationToken);

            // Generate summary
            var summary = await _openAIService.SummarizeMeetingAsync(transcriptionText, cancellationToken);
            transcription.Summary = summary;
            transcription.UpdatedAt = DateTime.UtcNow;
            await _transcriptionRepository.UpdateAsync(transcription, cancellationToken);

            // Update status
            transcription.Status = TranscriptionStatus.ExtractingTasks;
            transcription.UpdatedAt = DateTime.UtcNow;
            await _transcriptionRepository.UpdateAsync(transcription, cancellationToken);

            // Extract tasks
            var tasks = await _openAIService.ExtractTasksAsync(transcriptionText, summary, cancellationToken);
            transcription.ExtractedTasks = tasks;
            
            // Mark as completed
            transcription.Status = TranscriptionStatus.Completed;
            transcription.EndTime = DateTime.UtcNow;
            transcription.UpdatedAt = DateTime.UtcNow;
            await _transcriptionRepository.UpdateAsync(transcription, cancellationToken);

            _logger.LogInformation("Transcription completed successfully for ID: {TranscriptionId}", transcription.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing transcription for ID: {TranscriptionId}", transcription.Id);
            transcription.Status = TranscriptionStatus.Failed;
            transcription.UpdatedAt = DateTime.UtcNow;
            await _transcriptionRepository.UpdateAsync(transcription, cancellationToken);
        }
    }
}