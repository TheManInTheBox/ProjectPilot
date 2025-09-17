namespace ProjectPilot.Core.Models;

public class TaskItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public string AssignedTo { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public List<string> Labels { get; set; } = new();
    public string MilestoneTitle { get; set; } = string.Empty;
    public TaskStatus Status { get; set; } = TaskStatus.Open;
    public string? GitHubIssueNumber { get; set; }
    public string? GitHubIssueUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum TaskPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public enum TaskStatus
{
    Open,
    InProgress,
    Completed,
    Cancelled
}