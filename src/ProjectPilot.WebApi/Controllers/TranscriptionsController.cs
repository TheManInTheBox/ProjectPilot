using Microsoft.AspNetCore.Mvc;
using ProjectPilot.Application.DTOs;
using ProjectPilot.Core.Interfaces;
using ProjectPilot.Core.Models;

namespace ProjectPilot.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TranscriptionsController : ControllerBase
{
    private readonly ITranscriptionService _transcriptionService;
    private readonly ILogger<TranscriptionsController> _logger;

    public TranscriptionsController(
        ITranscriptionService transcriptionService,
        ILogger<TranscriptionsController> logger)
    {
        _transcriptionService = transcriptionService;
        _logger = logger;
    }

    /// <summary>
    /// Upload audio file for transcription
    /// </summary>
    [HttpPost("upload")]
    public async Task<ActionResult<TranscriptionResponseDto>> UploadAudioForTranscription(
        [FromForm] IFormFile audioFile,
        [FromForm] string title = "",
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (audioFile == null || audioFile.Length == 0)
            {
                return BadRequest("Audio file is required");
            }

            _logger.LogInformation("Received audio upload request: {FileName}, Size: {Size} bytes", 
                audioFile.FileName, audioFile.Length);

            using var stream = audioFile.OpenReadStream();
            var transcription = await _transcriptionService.StartTranscriptionAsync(
                audioFile.FileName, 
                stream, 
                title, 
                cancellationToken);

            var response = MapToResponseDto(transcription);
            
            return Accepted($"api/transcriptions/{transcription.Id}", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing audio upload");
            return StatusCode(500, "Error processing audio file");
        }
    }

    /// <summary>
    /// Start transcription from audio URL
    /// </summary>
    [HttpPost("from-url")]
    public async Task<ActionResult<TranscriptionResponseDto>> TranscribeFromUrl(
        [FromBody] TranscriptionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(request.AudioUrl))
            {
                return BadRequest("Audio URL is required");
            }

            _logger.LogInformation("Received URL transcription request: {AudioUrl}", request.AudioUrl);

            // For URL-based transcription, we would need to download the file first
            // This is a simplified implementation
            using var httpClient = new HttpClient();
            using var audioStream = await httpClient.GetStreamAsync(request.AudioUrl, cancellationToken);

            var transcription = await _transcriptionService.StartTranscriptionAsync(
                request.AudioUrl,
                audioStream,
                request.Title,
                cancellationToken);

            var response = MapToResponseDto(transcription);
            
            return Accepted($"api/transcriptions/{transcription.Id}", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing URL transcription request");
            return StatusCode(500, "Error processing audio URL");
        }
    }

    /// <summary>
    /// Get transcription by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<TranscriptionResponseDto>> GetTranscription(
        string id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var transcription = await _transcriptionService.GetTranscriptionAsync(id, cancellationToken);
            var response = MapToResponseDto(transcription);
            return Ok(response);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Transcription with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving transcription {TranscriptionId}", id);
            return StatusCode(500, "Error retrieving transcription");
        }
    }

    /// <summary>
    /// Get all transcriptions with pagination
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TranscriptionResponseDto>>> GetTranscriptions(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var transcriptions = await _transcriptionService.GetTranscriptionsAsync(skip, take, cancellationToken);
            var response = transcriptions.Select(MapToResponseDto);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving transcriptions");
            return StatusCode(500, "Error retrieving transcriptions");
        }
    }

    /// <summary>
    /// Delete transcription
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteTranscription(
        string id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _transcriptionService.DeleteTranscriptionAsync(id, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Transcription with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting transcription {TranscriptionId}", id);
            return StatusCode(500, "Error deleting transcription");
        }
    }

    private static TranscriptionResponseDto MapToResponseDto(MeetingTranscription transcription)
    {
        return new TranscriptionResponseDto
        {
            Id = transcription.Id,
            Title = transcription.Title,
            StartTime = transcription.StartTime,
            EndTime = transcription.EndTime,
            TranscriptionText = transcription.TranscriptionText,
            Status = transcription.Status.ToString(),
            Summary = transcription.Summary,
            ExtractedTasks = transcription.ExtractedTasks.Select(t => new TaskItemDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                Priority = t.Priority.ToString(),
                AssignedTo = t.AssignedTo,
                DueDate = t.DueDate,
                Labels = t.Labels,
                MilestoneTitle = t.MilestoneTitle,
                Status = t.Status.ToString(),
                GitHubIssueNumber = t.GitHubIssueNumber,
                GitHubIssueUrl = t.GitHubIssueUrl,
                CreatedAt = t.CreatedAt
            }).ToList(),
            CreatedAt = transcription.CreatedAt,
            UpdatedAt = transcription.UpdatedAt
        };
    }
}