using Microsoft.AspNetCore.Mvc;
using ProjectPilot.Application.DTOs;
using ProjectPilot.Core.Interfaces;
using ProjectPilot.Core.Models;

namespace ProjectPilot.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GitHubController : ControllerBase
{
    private readonly IGitHubIntegrationService _gitHubIntegrationService;
    private readonly ITranscriptionService _transcriptionService;
    private readonly ILogger<GitHubController> _logger;

    public GitHubController(
        IGitHubIntegrationService gitHubIntegrationService,
        ITranscriptionService transcriptionService,
        ILogger<GitHubController> logger)
    {
        _gitHubIntegrationService = gitHubIntegrationService;
        _transcriptionService = transcriptionService;
        _logger = logger;
    }

    /// <summary>
    /// Sync tasks from a transcription to GitHub issues
    /// </summary>
    [HttpPost("sync")]
    public async Task<ActionResult<GitHubSyncResponseDto>> SyncTasksToGitHub(
        [FromBody] GitHubSyncRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting GitHub sync for {TaskCount} tasks to repository: {Owner}/{Repo}", 
                request.TaskIds.Count, request.Owner, request.Repository);

            var repository = new GitHubRepository
            {
                Owner = request.Owner,
                Name = request.Repository,
                Token = request.Token
            };

            // Validate repository access first
            var hasAccess = await _gitHubIntegrationService.ValidateRepositoryAccessAsync(repository, cancellationToken);
            if (!hasAccess)
            {
                return BadRequest("Unable to access GitHub repository. Please check your token and repository permissions.");
            }

            var tasks = new List<TaskItem>();

            // Retrieve tasks from transcriptions
            foreach (var taskId in request.TaskIds)
            {
                try
                {
                    // In a real implementation, you'd have a task service to get tasks by ID
                    // For now, we'll simulate this by looking through transcriptions
                    var transcriptions = await _transcriptionService.GetTranscriptionsAsync(0, 100, cancellationToken);
                    var task = transcriptions
                        .SelectMany(t => t.ExtractedTasks)
                        .FirstOrDefault(t => t.Id == taskId);

                    if (task != null)
                    {
                        tasks.Add(task);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not find task with ID: {TaskId}", taskId);
                }
            }

            if (!tasks.Any())
            {
                return BadRequest("No valid tasks found for the provided task IDs");
            }

            // Sync tasks to GitHub
            var githubIssues = await _gitHubIntegrationService.SyncTasksToGitHubAsync(repository, tasks, cancellationToken);

            var response = new GitHubSyncResponseDto
            {
                Success = githubIssues.Any(),
                CreatedIssues = githubIssues.Select(MapToGitHubIssueDto).ToList(),
                Errors = new List<string>()
            };

            if (githubIssues.Count < tasks.Count)
            {
                response.Errors.Add($"Only {githubIssues.Count} out of {tasks.Count} tasks were successfully synced");
            }

            _logger.LogInformation("GitHub sync completed. {SuccessCount}/{TotalCount} tasks synced successfully", 
                githubIssues.Count, tasks.Count);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing tasks to GitHub");
            return StatusCode(500, new GitHubSyncResponseDto
            {
                Success = false,
                Errors = new List<string> { "Error syncing tasks to GitHub" }
            });
        }
    }

    /// <summary>
    /// Validate access to a GitHub repository
    /// </summary>
    [HttpPost("validate-repository")]
    public async Task<ActionResult<object>> ValidateRepository(
        [FromBody] GitHubRepository repository,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Validating repository access: {Owner}/{Repo}", repository.Owner, repository.Name);

            var hasAccess = await _gitHubIntegrationService.ValidateRepositoryAccessAsync(repository, cancellationToken);

            return Ok(new { hasAccess, repository = $"{repository.Owner}/{repository.Name}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating repository access");
            return StatusCode(500, new { hasAccess = false, error = "Error validating repository access" });
        }
    }

    /// <summary>
    /// Create a single GitHub issue from a task
    /// </summary>
    [HttpPost("create-issue")]
    public async Task<ActionResult<GitHubIssueDto>> CreateGitHubIssue(
        [FromBody] CreateIssueRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating GitHub issue for task: {TaskId}", request.TaskId);

            var repository = new GitHubRepository
            {
                Owner = request.Owner,
                Name = request.Repository,
                Token = request.Token
            };

            // Find the task
            var transcriptions = await _transcriptionService.GetTranscriptionsAsync(0, 100, cancellationToken);
            var task = transcriptions
                .SelectMany(t => t.ExtractedTasks)
                .FirstOrDefault(t => t.Id == request.TaskId);

            if (task == null)
            {
                return NotFound($"Task with ID {request.TaskId} not found");
            }

            var githubIssue = await _gitHubIntegrationService.CreateGitHubIssueAsync(repository, task, cancellationToken);
            var response = MapToGitHubIssueDto(githubIssue);

            return CreatedAtAction(nameof(CreateGitHubIssue), new { id = githubIssue.Number }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating GitHub issue for task: {TaskId}", request.TaskId);
            return StatusCode(500, "Error creating GitHub issue");
        }
    }

    private static GitHubIssueDto MapToGitHubIssueDto(GitHubIssue issue)
    {
        return new GitHubIssueDto
        {
            Number = issue.Number,
            Title = issue.Title,
            Body = issue.Body,
            State = issue.State,
            Labels = issue.Labels,
            Assignee = issue.Assignee,
            Milestone = issue.Milestone,
            HtmlUrl = issue.HtmlUrl,
            CreatedAt = issue.CreatedAt,
            UpdatedAt = issue.UpdatedAt
        };
    }
}

public class CreateIssueRequestDto
{
    public string Owner { get; set; } = string.Empty;
    public string Repository { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string TaskId { get; set; } = string.Empty;
}