namespace ProjectPilot.Application.DTOs;

public class TranscriptionRequestDto
{
    public string Title { get; set; } = string.Empty;
    public string AudioFileName { get; set; } = string.Empty;
    public Stream? AudioStream { get; set; }
    public string? AudioUrl { get; set; }
}

public class TranscriptionResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string TranscriptionText { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public List<TaskItemDto> ExtractedTasks { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class TaskItemDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string AssignedTo { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public List<string> Labels { get; set; } = new();
    public string MilestoneTitle { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? GitHubIssueNumber { get; set; }
    public string? GitHubIssueUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class GitHubSyncRequestDto
{
    public string Owner { get; set; } = string.Empty;
    public string Repository { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public List<string> TaskIds { get; set; } = new();
}

public class GitHubSyncResponseDto
{
    public bool Success { get; set; }
    public List<GitHubIssueDto> CreatedIssues { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

public class GitHubIssueDto
{
    public int Number { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public List<string> Labels { get; set; } = new();
    public string? Assignee { get; set; }
    public string? Milestone { get; set; }
    public string HtmlUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}