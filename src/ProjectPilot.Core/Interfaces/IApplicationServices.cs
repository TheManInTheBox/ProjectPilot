using ProjectPilot.Core.Models;

namespace ProjectPilot.Core.Interfaces;

public interface ITranscriptionService
{
    Task<MeetingTranscription> StartTranscriptionAsync(string audioFileName, Stream audioStream, string title = "", CancellationToken cancellationToken = default);
    Task<MeetingTranscription> GetTranscriptionAsync(string id, CancellationToken cancellationToken = default);
    Task<List<MeetingTranscription>> GetTranscriptionsAsync(int skip = 0, int take = 20, CancellationToken cancellationToken = default);
    Task<MeetingTranscription> UpdateTranscriptionAsync(MeetingTranscription transcription, CancellationToken cancellationToken = default);
    Task DeleteTranscriptionAsync(string id, CancellationToken cancellationToken = default);
}

public interface ITaskExtractionService
{
    Task<List<TaskItem>> ExtractTasksFromTranscriptionAsync(string transcription, string summary, CancellationToken cancellationToken = default);
    Task<TaskItem> UpdateTaskAsync(TaskItem task, CancellationToken cancellationToken = default);
}

public interface IGitHubIntegrationService
{
    Task<List<GitHubIssue>> SyncTasksToGitHubAsync(GitHubRepository repository, List<TaskItem> tasks, CancellationToken cancellationToken = default);
    Task<GitHubIssue> CreateGitHubIssueAsync(GitHubRepository repository, TaskItem task, CancellationToken cancellationToken = default);
    Task<bool> ValidateRepositoryAccessAsync(GitHubRepository repository, CancellationToken cancellationToken = default);
}

public interface IRepository<T> where T : class
{
    Task<T> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<List<T>> GetAllAsync(int skip = 0, int take = 20, CancellationToken cancellationToken = default);
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
    Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}