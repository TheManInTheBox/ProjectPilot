using Microsoft.Extensions.Logging;
using ProjectPilot.Application.DTOs;
using ProjectPilot.Core.Interfaces;
using ProjectPilot.Core.Models;

namespace ProjectPilot.Application.Services;

public class GitHubIntegrationOrchestrationService : IGitHubIntegrationService
{
    private readonly IGitHubService _gitHubService;
    private readonly IOpenAIService _openAIService;
    private readonly ILogger<GitHubIntegrationOrchestrationService> _logger;

    public GitHubIntegrationOrchestrationService(
        IGitHubService gitHubService,
        IOpenAIService openAIService,
        ILogger<GitHubIntegrationOrchestrationService> logger)
    {
        _gitHubService = gitHubService;
        _openAIService = openAIService;
        _logger = logger;
    }

    public async Task<List<GitHubIssue>> SyncTasksToGitHubAsync(GitHubRepository repository, List<TaskItem> tasks, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting sync of {TaskCount} tasks to GitHub repository: {Owner}/{Repo}", tasks.Count, repository.Owner, repository.Name);

            var results = new List<GitHubIssue>();

            foreach (var task in tasks)
            {
                try
                {
                    var githubIssue = await CreateGitHubIssueAsync(repository, task, cancellationToken);
                    results.Add(githubIssue);
                    
                    // Update task with GitHub issue information
                    task.GitHubIssueNumber = githubIssue.Number.ToString();
                    task.GitHubIssueUrl = githubIssue.HtmlUrl;

                    _logger.LogInformation("Successfully synced task '{TaskTitle}' to GitHub issue #{IssueNumber}", task.Title, githubIssue.Number);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to sync task '{TaskTitle}' to GitHub", task.Title);
                    // Continue with other tasks rather than failing entirely
                }
            }

            _logger.LogInformation("Completed sync. {SuccessCount}/{TotalCount} tasks synced successfully", results.Count, tasks.Count);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing tasks to GitHub repository: {Owner}/{Repo}", repository.Owner, repository.Name);
            throw;
        }
    }

    public async Task<GitHubIssue> CreateGitHubIssueAsync(GitHubRepository repository, TaskItem task, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating GitHub issue for task: {TaskTitle}", task.Title);

            // Generate enhanced title if needed
            var title = await _openAIService.GenerateIssueTitleAsync(task.Description, cancellationToken);
            if (!string.IsNullOrEmpty(title) && title.Length <= 100)
            {
                task.Title = title;
            }

            // Generate enhanced body
            var body = await _openAIService.GenerateIssueBodyAsync(task.Description, $"Meeting task: {task.Title}", cancellationToken);
            if (!string.IsNullOrEmpty(body))
            {
                task.Description = body;
            }

            // Create the GitHub issue
            var githubIssue = await _gitHubService.CreateIssueAsync(repository, task, cancellationToken);

            _logger.LogInformation("Successfully created GitHub issue #{IssueNumber}: {IssueTitle}", githubIssue.Number, githubIssue.Title);
            return githubIssue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating GitHub issue for task: {TaskTitle}", task.Title);
            throw;
        }
    }

    public async Task<bool> ValidateRepositoryAccessAsync(GitHubRepository repository, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Validating access to GitHub repository: {Owner}/{Repo}", repository.Owner, repository.Name);

            // Try to get repository issues to validate access
            var issues = await _gitHubService.GetRepositoryIssuesAsync(repository, cancellationToken);

            _logger.LogInformation("Successfully validated access to GitHub repository: {Owner}/{Repo}", repository.Owner, repository.Name);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate access to GitHub repository: {Owner}/{Repo}", repository.Owner, repository.Name);
            return false;
        }
    }
}