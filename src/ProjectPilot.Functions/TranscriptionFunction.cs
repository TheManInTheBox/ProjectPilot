using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ProjectPilot.Core.Interfaces;
using ProjectPilot.Core.Models;
using System.Net;
using System.Text.Json;

namespace ProjectPilot.Functions;

public class TranscriptionFunction
{
    private readonly ILogger<TranscriptionFunction> _logger;
    private readonly ITranscriptionService _transcriptionService;
    private readonly IGitHubIntegrationService _gitHubIntegrationService;

    public TranscriptionFunction(
        ILogger<TranscriptionFunction> logger,
        ITranscriptionService transcriptionService,
        IGitHubIntegrationService gitHubIntegrationService)
    {
        _logger = logger;
        _transcriptionService = transcriptionService;
        _gitHubIntegrationService = gitHubIntegrationService;
    }

    [Function("ProcessTranscriptionQueue")]
    public async Task ProcessTranscriptionQueue([QueueTrigger("transcriptions")] string queueMessage)
    {
        try
        {
            _logger.LogInformation("Processing transcription queue message: {Message}", queueMessage);

            var transcriptionRequest = JsonSerializer.Deserialize<TranscriptionQueueMessage>(queueMessage);
            if (transcriptionRequest == null)
            {
                _logger.LogError("Failed to deserialize transcription queue message");
                return;
            }

            // Process the transcription
            // In a real implementation, you would retrieve the audio file from storage
            // and process it through the transcription service
            _logger.LogInformation("Transcription processing completed for ID: {TranscriptionId}", transcriptionRequest.TranscriptionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing transcription queue message: {Message}", queueMessage);
            throw; // Re-throw to trigger retry logic
        }
    }

    [Function("SyncTasksToGitHub")]
    public async Task SyncTasksToGitHub([QueueTrigger("github-sync")] string queueMessage)
    {
        try
        {
            _logger.LogInformation("Processing GitHub sync queue message: {Message}", queueMessage);

            var syncRequest = JsonSerializer.Deserialize<GitHubSyncQueueMessage>(queueMessage);
            if (syncRequest == null)
            {
                _logger.LogError("Failed to deserialize GitHub sync queue message");
                return;
            }

            var repository = new GitHubRepository
            {
                Owner = syncRequest.Owner,
                Name = syncRequest.Repository,
                Token = syncRequest.Token
            };

            // In a real implementation, you would retrieve the tasks from storage
            var tasks = new List<TaskItem>(); // Retrieve tasks by IDs

            await _gitHubIntegrationService.SyncTasksToGitHubAsync(repository, tasks);

            _logger.LogInformation("GitHub sync completed for {TaskCount} tasks", tasks.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing GitHub sync queue message: {Message}", queueMessage);
            throw; // Re-throw to trigger retry logic
        }
    }

    [Function("ScheduledTranscriptionCleanup")]
    public async Task ScheduledTranscriptionCleanup([TimerTrigger("0 0 2 * * *")] TimerInfo timer)
    {
        _logger.LogInformation("Starting scheduled transcription cleanup at: {Time}", DateTime.UtcNow);

        try
        {
            // Clean up old transcription records
            var transcriptions = await _transcriptionService.GetTranscriptionsAsync(0, 1000);
            var oldTranscriptions = transcriptions.Where(t => t.CreatedAt < DateTime.UtcNow.AddDays(-30));

            foreach (var transcription in oldTranscriptions)
            {
                await _transcriptionService.DeleteTranscriptionAsync(transcription.Id);
                _logger.LogInformation("Deleted old transcription: {TranscriptionId}", transcription.Id);
            }

            _logger.LogInformation("Transcription cleanup completed. Deleted {Count} old records", oldTranscriptions.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during scheduled transcription cleanup");
        }
    }

    [Function("HealthCheck")]
    public static async Task<HttpResponseData> HealthCheck(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequestData req)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new { status = "healthy", timestamp = DateTime.UtcNow });
        return response;
    }
}

public class TranscriptionQueueMessage
{
    public string TranscriptionId { get; set; } = string.Empty;
    public string AudioFileName { get; set; } = string.Empty;
    public string AudioUrl { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
}

public class GitHubSyncQueueMessage
{
    public string Owner { get; set; } = string.Empty;
    public string Repository { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public List<string> TaskIds { get; set; } = new();
}