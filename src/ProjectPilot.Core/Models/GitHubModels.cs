namespace ProjectPilot.Core.Models;

public class GitHubRepository
{
    public string Owner { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string DefaultMilestone { get; set; } = string.Empty;
    public List<string> DefaultLabels { get; set; } = new();
}

public class GitHubIssue
{
    public int Number { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string State { get; set; } = "open";
    public List<string> Labels { get; set; } = new();
    public string? Assignee { get; set; }
    public string? Milestone { get; set; }
    public string HtmlUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}