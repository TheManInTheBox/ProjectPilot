using ProjectPilot.Core.Models;

namespace ProjectPilot.Core.Interfaces;

public interface ISpeechToTextService
{
    Task<string> TranscribeAudioAsync(Stream audioStream, string fileName, CancellationToken cancellationToken = default);
    Task<string> TranscribeAudioFromUrlAsync(string audioUrl, CancellationToken cancellationToken = default);
}

public interface IOpenAIService
{
    Task<string> SummarizeMeetingAsync(string transcription, CancellationToken cancellationToken = default);
    Task<List<TaskItem>> ExtractTasksAsync(string transcription, string summary, CancellationToken cancellationToken = default);
    Task<string> GenerateIssueTitleAsync(string taskDescription, CancellationToken cancellationToken = default);
    Task<string> GenerateIssueBodyAsync(string taskDescription, string meetingContext, CancellationToken cancellationToken = default);
}

public interface IGitHubService
{
    Task<GitHubIssue> CreateIssueAsync(GitHubRepository repository, TaskItem task, CancellationToken cancellationToken = default);
    Task<List<GitHubIssue>> GetRepositoryIssuesAsync(GitHubRepository repository, CancellationToken cancellationToken = default);
    Task<GitHubIssue> UpdateIssueAsync(GitHubRepository repository, int issueNumber, TaskItem task, CancellationToken cancellationToken = default);
    Task<List<string>> GetRepositoryMilestonesAsync(GitHubRepository repository, CancellationToken cancellationToken = default);
    Task<string> CreateMilestoneAsync(GitHubRepository repository, string title, string description, DateTime? dueDate = null, CancellationToken cancellationToken = default);
}