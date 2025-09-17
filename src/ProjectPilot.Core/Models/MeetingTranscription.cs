namespace ProjectPilot.Core.Models;

public class MeetingTranscription
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string AudioFileName { get; set; } = string.Empty;
    public string TranscriptionText { get; set; } = string.Empty;
    public TranscriptionStatus Status { get; set; } = TranscriptionStatus.Pending;
    public List<TaskItem> ExtractedTasks { get; set; } = new();
    public string Summary { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public enum TranscriptionStatus
{
    Pending,
    InProgress,
    Transcribing,
    Summarizing,
    ExtractingTasks,
    SyncingToGitHub,
    Completed,
    Failed
}