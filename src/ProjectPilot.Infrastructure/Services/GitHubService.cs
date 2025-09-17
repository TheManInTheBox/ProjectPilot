using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Octokit;
using ProjectPilot.Core.Interfaces;
using ProjectPilot.Core.Models;
using ProjectPilot.Infrastructure.Configuration;

namespace ProjectPilot.Infrastructure.Services;

public class GitHubService : IGitHubService
{
    private readonly GitHubOptions _options;
    private readonly ILogger<GitHubService> _logger;

    public GitHubService(IOptions<GitHubOptions> options, ILogger<GitHubService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<GitHubIssue> CreateIssueAsync(GitHubRepository repository, TaskItem task, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating GitHub issue for task: {TaskTitle}", task.Title);

            var client = CreateGitHubClient(repository.Token);
            
            var newIssue = new NewIssue(task.Title)
            {
                Body = task.Description
            };

            // Add labels
            foreach (var label in task.Labels)
            {
                newIssue.Labels.Add(label);
            }

            // Set assignee if specified
            if (!string.IsNullOrEmpty(task.AssignedTo))
            {
                newIssue.Assignees.Add(task.AssignedTo);
            }

            // Set milestone if specified
            if (!string.IsNullOrEmpty(task.MilestoneTitle))
            {
                var milestones = await client.Issue.Milestone.GetAllForRepository(repository.Owner, repository.Name);
                var milestone = milestones.FirstOrDefault(m => m.Title.Equals(task.MilestoneTitle, StringComparison.OrdinalIgnoreCase));
                if (milestone != null)
                {
                    newIssue.Milestone = milestone.Number;
                }
            }

            var createdIssue = await client.Issue.Create(repository.Owner, repository.Name, newIssue);

            var result = new GitHubIssue
            {
                Number = createdIssue.Number,
                Title = createdIssue.Title,
                Body = createdIssue.Body ?? string.Empty,
                State = createdIssue.State.ToString().ToLower(),
                Labels = createdIssue.Labels.Select(l => l.Name).ToList(),
                Assignee = createdIssue.Assignee?.Login,
                Milestone = createdIssue.Milestone?.Title,
                HtmlUrl = createdIssue.HtmlUrl,
                CreatedAt = DateTime.UtcNow, // createdIssue.CreatedAt conversion needed
                UpdatedAt = DateTime.UtcNow  // createdIssue.UpdatedAt conversion needed
            };

            _logger.LogInformation("Successfully created GitHub issue #{IssueNumber}: {IssueTitle}", result.Number, result.Title);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating GitHub issue for task: {TaskTitle}", task.Title);
            throw;
        }
    }

    public async Task<List<GitHubIssue>> GetRepositoryIssuesAsync(GitHubRepository repository, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving issues for repository: {Owner}/{Repo}", repository.Owner, repository.Name);

            var client = CreateGitHubClient(repository.Token);
            var issues = await client.Issue.GetAllForRepository(repository.Owner, repository.Name);

            return issues.Select(issue => new GitHubIssue
            {
                Number = issue.Number,
                Title = issue.Title,
                Body = issue.Body ?? string.Empty,
                State = issue.State.ToString().ToLower(),
                Labels = issue.Labels.Select(l => l.Name).ToList(),
                Assignee = issue.Assignee?.Login,
                Milestone = issue.Milestone?.Title,
                HtmlUrl = issue.HtmlUrl,
                CreatedAt = DateTime.UtcNow, // issue.CreatedAt conversion needed
                UpdatedAt = DateTime.UtcNow  // issue.UpdatedAt conversion needed
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving issues for repository: {Owner}/{Repo}", repository.Owner, repository.Name);
            throw;
        }
    }

    public async Task<GitHubIssue> UpdateIssueAsync(GitHubRepository repository, int issueNumber, TaskItem task, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating GitHub issue #{IssueNumber}", issueNumber);

            var client = CreateGitHubClient(repository.Token);
            
            var issueUpdate = new IssueUpdate
            {
                Title = task.Title,
                Body = task.Description,
                State = task.Status == Core.Models.TaskStatus.Completed ? ItemState.Closed : ItemState.Open
            };

            var updatedIssue = await client.Issue.Update(repository.Owner, repository.Name, issueNumber, issueUpdate);

            var result = new GitHubIssue
            {
                Number = updatedIssue.Number,
                Title = updatedIssue.Title,
                Body = updatedIssue.Body ?? string.Empty,
                State = updatedIssue.State.ToString().ToLower(),
                Labels = updatedIssue.Labels.Select(l => l.Name).ToList(),
                Assignee = updatedIssue.Assignee?.Login,
                Milestone = updatedIssue.Milestone?.Title,
                HtmlUrl = updatedIssue.HtmlUrl,
                CreatedAt = DateTime.UtcNow, // updatedIssue.CreatedAt conversion needed
                UpdatedAt = DateTime.UtcNow  // updatedIssue.UpdatedAt conversion needed
            };

            _logger.LogInformation("Successfully updated GitHub issue #{IssueNumber}: {IssueTitle}", result.Number, result.Title);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating GitHub issue #{IssueNumber}", issueNumber);
            throw;
        }
    }

    public async Task<List<string>> GetRepositoryMilestonesAsync(GitHubRepository repository, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving milestones for repository: {Owner}/{Repo}", repository.Owner, repository.Name);

            var client = CreateGitHubClient(repository.Token);
            var milestones = await client.Issue.Milestone.GetAllForRepository(repository.Owner, repository.Name);

            return milestones.Select(m => m.Title).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving milestones for repository: {Owner}/{Repo}", repository.Owner, repository.Name);
            throw;
        }
    }

    public async Task<string> CreateMilestoneAsync(GitHubRepository repository, string title, string description, DateTime? dueDate = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating milestone: {MilestoneTitle} for repository: {Owner}/{Repo}", title, repository.Owner, repository.Name);

            var client = CreateGitHubClient(repository.Token);
            
            var newMilestone = new NewMilestone(title)
            {
                Description = description
            };

            if (dueDate.HasValue)
            {
                newMilestone.DueOn = dueDate.Value;
            }

            var createdMilestone = await client.Issue.Milestone.Create(repository.Owner, repository.Name, newMilestone);

            _logger.LogInformation("Successfully created milestone: {MilestoneTitle}", createdMilestone.Title);
            return createdMilestone.Title;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating milestone: {MilestoneTitle}", title);
            throw;
        }
    }

    private GitHubClient CreateGitHubClient(string token)
    {
        var client = new GitHubClient(new ProductHeaderValue(_options.UserAgent));
        
        var tokenToUse = !string.IsNullOrEmpty(token) ? token : _options.DefaultToken;
        if (!string.IsNullOrEmpty(tokenToUse))
        {
            client.Credentials = new Credentials(tokenToUse);
        }

        return client;
    }
}