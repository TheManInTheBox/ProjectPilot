using Microsoft.Extensions.Logging;
using ProjectPilot.Core.Interfaces;
using ProjectPilot.Core.Models;

namespace ProjectPilot.Application.Services;

public class TaskExtractionOrchestrationService : ITaskExtractionService
{
    private readonly IOpenAIService _openAIService;
    private readonly ILogger<TaskExtractionOrchestrationService> _logger;

    public TaskExtractionOrchestrationService(
        IOpenAIService openAIService,
        ILogger<TaskExtractionOrchestrationService> logger)
    {
        _openAIService = openAIService;
        _logger = logger;
    }

    public async Task<List<TaskItem>> ExtractTasksFromTranscriptionAsync(string transcription, string summary, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting task extraction from transcription and summary");

            var tasks = await _openAIService.ExtractTasksAsync(transcription, summary, cancellationToken);

            // Post-process tasks
            foreach (var task in tasks)
            {
                // Ensure task has an ID
                if (string.IsNullOrEmpty(task.Id))
                {
                    task.Id = Guid.NewGuid().ToString();
                }

                // Set creation time
                if (task.CreatedAt == default)
                {
                    task.CreatedAt = DateTime.UtcNow;
                }

                // Validate and clean up task data
                ValidateAndCleanTask(task);
            }

            _logger.LogInformation("Successfully extracted {TaskCount} tasks", tasks.Count);
            return tasks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting tasks from transcription");
            throw;
        }
    }

    public async Task<TaskItem> UpdateTaskAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating task: {TaskTitle}", task.Title);

            // Validate and clean up task data
            ValidateAndCleanTask(task);

            _logger.LogInformation("Successfully updated task: {TaskTitle}", task.Title);
            return task;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating task: {TaskTitle}", task.Title);
            throw;
        }
    }

    private void ValidateAndCleanTask(TaskItem task)
    {
        // Ensure title is not too long
        if (task.Title.Length > 100)
        {
            task.Title = task.Title[..97] + "...";
        }

        // Ensure description is reasonable length
        if (task.Description.Length > 10000)
        {
            task.Description = task.Description[..9997] + "...";
        }

        // Validate priority
        if (!Enum.IsDefined(typeof(TaskPriority), task.Priority))
        {
            task.Priority = TaskPriority.Medium;
        }

        // Clean up labels (remove empty ones, limit count)
        task.Labels = task.Labels
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Take(10) // GitHub has a limit on labels
            .ToList();

        // Validate milestone title length
        if (task.MilestoneTitle.Length > 100)
        {
            task.MilestoneTitle = task.MilestoneTitle[..97] + "...";
        }
    }
}