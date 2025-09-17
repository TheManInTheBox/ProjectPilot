namespace ProjectPilot.Infrastructure.Configuration;

public class AzureSpeechOptions
{
    public string SubscriptionKey { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string Language { get; set; } = "en-US";
}

public class AzureOpenAIOptions
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string DeploymentName { get; set; } = string.Empty;
    public string ModelName { get; set; } = "gpt-4";
}

public class GitHubOptions
{
    public string DefaultToken { get; set; } = string.Empty;
    public string UserAgent { get; set; } = "ProjectPilot/1.0";
}

public class CosmosDbOptions
{
    public string Endpoint { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = "ProjectPilot";
    public string TranscriptionsContainerName { get; set; } = "Transcriptions";
    public string TasksContainerName { get; set; } = "Tasks";
}