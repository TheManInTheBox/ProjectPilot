using Azure.AI.OpenAI;
using Azure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjectPilot.Core.Interfaces;
using ProjectPilot.Core.Models;
using ProjectPilot.Infrastructure.Configuration;
using System.Text.Json;

namespace ProjectPilot.Infrastructure.Services;

public class AzureOpenAIService : IOpenAIService
{
    private readonly AzureOpenAIOptions _options;
    private readonly ILogger<AzureOpenAIService> _logger;
    private readonly OpenAIClient _openAIClient;

    public AzureOpenAIService(IOptions<AzureOpenAIOptions> options, ILogger<AzureOpenAIService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _openAIClient = new OpenAIClient(new Uri(_options.Endpoint), new AzureKeyCredential(_options.ApiKey));
    }

    public async Task<string> SummarizeMeetingAsync(string transcription, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting meeting summarization");

            var chatCompletionsOptions = new ChatCompletionsOptions()
            {
                DeploymentName = _options.DeploymentName,
                Messages =
                {
                    new ChatRequestSystemMessage(
                        "You are an AI assistant specialized in summarizing meeting transcriptions. " +
                        "Create a concise, well-structured summary that captures key discussion points, " +
                        "decisions made, and important topics covered. Focus on actionable insights."
                    ),
                    new ChatRequestUserMessage($"Please summarize this meeting transcription:\n\n{transcription}")
                }
            };

            var response = await _openAIClient.GetChatCompletionsAsync(chatCompletionsOptions, cancellationToken);
            var summary = response.Value.Choices[0].Message.Content;

            _logger.LogInformation("Meeting summarization completed successfully");
            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error summarizing meeting transcription");
            throw;
        }
    }

    public async Task<List<TaskItem>> ExtractTasksAsync(string transcription, string summary, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting task extraction from transcription and summary");

            var systemPrompt = @"You are an AI assistant that extracts actionable tasks from meeting transcriptions and summaries.
Extract tasks that are:
- Specific and actionable
- Have clear deliverables
- Can be assigned to team members
- Have implied or explicit deadlines

Return a JSON array of tasks with this exact structure:
[
  {
    ""Title"": ""Task title (max 100 characters)"",
    ""Description"": ""Detailed description"",
    ""Priority"": 1-4 (1=Low, 2=Medium, 3=High, 4=Critical),
    ""AssignedTo"": ""Person mentioned or empty string"",
    ""DueDate"": ""ISO date or null"",
    ""Labels"": [""relevant"", ""labels""],
    ""MilestoneTitle"": ""Related milestone or empty string""
  }
]

Only return the JSON array, no additional text.";

            var userPrompt = $@"Meeting Summary:
{summary}

Full Transcription:
{transcription}

Extract all actionable tasks from this meeting content:";

            var chatCompletionsOptions = new ChatCompletionsOptions()
            {
                DeploymentName = _options.DeploymentName,
                Messages =
                {
                    new ChatRequestSystemMessage(systemPrompt),
                    new ChatRequestUserMessage(userPrompt)
                }
            };

            var response = await _openAIClient.GetChatCompletionsAsync(chatCompletionsOptions, cancellationToken);
            var tasksJson = response.Value.Choices[0].Message.Content;

            // Parse the JSON response into TaskItem objects
            var taskData = JsonSerializer.Deserialize<List<TaskData>>(tasksJson);
            var tasks = taskData?.Select(t => new TaskItem
            {
                Title = t.Title,
                Description = t.Description,
                Priority = (TaskPriority)t.Priority,
                AssignedTo = t.AssignedTo,
                DueDate = t.DueDate,
                Labels = t.Labels?.ToList() ?? new List<string>(),
                MilestoneTitle = t.MilestoneTitle
            }).ToList() ?? new List<TaskItem>();

            _logger.LogInformation("Extracted {TaskCount} tasks from meeting content", tasks.Count);
            return tasks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting tasks from meeting content");
            return new List<TaskItem>();
        }
    }

    public async Task<string> GenerateIssueTitleAsync(string taskDescription, CancellationToken cancellationToken = default)
    {
        try
        {
            var chatCompletionsOptions = new ChatCompletionsOptions()
            {
                DeploymentName = _options.DeploymentName,
                Messages =
                {
                    new ChatRequestSystemMessage(
                        "Generate a concise, descriptive GitHub issue title (max 100 characters) " +
                        "based on the task description provided. Make it actionable and clear."
                    ),
                    new ChatRequestUserMessage($"Task: {taskDescription}")
                }
            };

            var response = await _openAIClient.GetChatCompletionsAsync(chatCompletionsOptions, cancellationToken);
            return response.Value.Choices[0].Message.Content.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating issue title");
            return taskDescription.Length > 100 ? taskDescription[..97] + "..." : taskDescription;
        }
    }

    public async Task<string> GenerateIssueBodyAsync(string taskDescription, string meetingContext, CancellationToken cancellationToken = default)
    {
        try
        {
            var chatCompletionsOptions = new ChatCompletionsOptions()
            {
                DeploymentName = _options.DeploymentName,
                Messages =
                {
                    new ChatRequestSystemMessage(
                        "Generate a well-formatted GitHub issue body with sections for Description, " +
                        "Acceptance Criteria, and Context. Use Markdown formatting."
                    ),
                    new ChatRequestUserMessage($"Task: {taskDescription}\n\nMeeting Context: {meetingContext}")
                }
            };

            var response = await _openAIClient.GetChatCompletionsAsync(chatCompletionsOptions, cancellationToken);
            return response.Value.Choices[0].Message.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating issue body");
            return $"## Description\n{taskDescription}\n\n## Context\nExtracted from meeting discussion.\n\n{meetingContext}";
        }
    }

    private class TaskData
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Priority { get; set; }
        public string AssignedTo { get; set; } = string.Empty;
        public DateTime? DueDate { get; set; }
        public string[]? Labels { get; set; }
        public string MilestoneTitle { get; set; } = string.Empty;
    }
}