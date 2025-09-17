# ProjectPilot

Automatically transcribes meetings, summarizes discussions, extracts tasks, and syncs them to GitHub issues and milestones. Powered by Azure Speech, Azure OpenAI, and GitHub REST API. Includes ASP.NET Core Web API, Azure Functions, and optional dashboard integration.

## Architecture

ProjectPilot follows clean architecture principles with the following layers:

- **Core**: Domain models, interfaces, and business rules
- **Infrastructure**: External service implementations (Azure Speech, OpenAI, GitHub)
- **Application**: Service orchestration and business logic
- **WebApi**: REST API controllers and HTTP endpoints
- **Functions**: Azure Functions for background processing and scheduled tasks

## Features

### 🎤 Audio Transcription
- Upload audio files for transcription using Azure Speech to Text
- Support for multiple audio formats
- Real-time transcription status updates
- Bulk transcription processing

### 🤖 AI-Powered Summarization
- Automatic meeting summaries using Azure OpenAI
- Key decision point extraction
- Action item identification
- Meeting context analysis

### ✅ Task Extraction
- Intelligent task detection from transcriptions
- Priority assignment and categorization
- Assignee identification from conversation context
- Due date inference

### 🔗 GitHub Integration
- Automatic GitHub issue creation from extracted tasks
- Milestone management and assignment
- Label application and organization
- Repository access validation

### 📊 Background Processing
- Queue-based transcription processing
- Scheduled cleanup tasks
- Error handling and retry logic
- Health monitoring

## Getting Started

### Prerequisites

- .NET 8.0 SDK
- Azure Speech Services account
- Azure OpenAI account
- GitHub personal access token
- Visual Studio 2022 or VS Code

### Configuration

1. Update `appsettings.Development.json` in the WebApi project:

```json
{
  "AzureSpeech": {
    "SubscriptionKey": "YOUR_AZURE_SPEECH_KEY",
    "Region": "eastus",
    "Language": "en-US"
  },
  "AzureOpenAI": {
    "Endpoint": "https://your-endpoint.openai.azure.com/",
    "ApiKey": "YOUR_AZURE_OPENAI_KEY",
    "DeploymentName": "gpt-4",
    "ModelName": "gpt-4"
  },
  "GitHub": {
    "DefaultToken": "YOUR_GITHUB_TOKEN",
    "UserAgent": "ProjectPilot/1.0"
  }
}
```

2. Update `local.settings.json` in the Functions project with the same values.

### Running the Application

#### Web API
```bash
cd src/ProjectPilot.WebApi
dotnet run
```

Access the API documentation at: `https://localhost:7xxx/swagger`

#### Azure Functions
```bash
cd src/ProjectPilot.Functions
func start
```

### API Endpoints

#### Transcription Endpoints

- `POST /api/transcriptions/upload` - Upload audio file for transcription
- `POST /api/transcriptions/from-url` - Start transcription from URL
- `GET /api/transcriptions/{id}` - Get transcription by ID
- `GET /api/transcriptions` - List all transcriptions (paginated)
- `DELETE /api/transcriptions/{id}` - Delete transcription

#### GitHub Integration Endpoints

- `POST /api/github/sync` - Sync tasks to GitHub issues
- `POST /api/github/validate-repository` - Validate repository access
- `POST /api/github/create-issue` - Create single GitHub issue

#### Health Check

- `GET /health` - Health check endpoint

## Usage Examples

### Upload Audio for Transcription

```bash
curl -X POST "https://localhost:7xxx/api/transcriptions/upload" \
  -H "Content-Type: multipart/form-data" \
  -F "audioFile=@meeting.wav" \
  -F "title=Weekly Team Meeting"
```

### Sync Tasks to GitHub

```bash
curl -X POST "https://localhost:7xxx/api/github/sync" \
  -H "Content-Type: application/json" \
  -d '{
    "owner": "your-username",
    "repository": "your-repo",
    "token": "your-github-token",
    "taskIds": ["task-id-1", "task-id-2"]
  }'
```

## Azure Functions

The solution includes several Azure Functions for background processing:

- **ProcessTranscriptionQueue**: Processes transcription requests from queue
- **SyncTasksToGitHub**: Handles GitHub synchronization in background
- **ScheduledTranscriptionCleanup**: Cleans up old transcription records (runs daily at 2 AM)
- **HealthCheck**: HTTP-triggered health check function

## Testing

### Unit Tests

```bash
dotnet test tests/ProjectPilot.Core.Tests
dotnet test tests/ProjectPilot.Infrastructure.Tests
```

### Manual Testing

1. Start the Web API
2. Use the Swagger UI to test endpoints
3. Upload a sample audio file
4. Check transcription status
5. Verify GitHub integration

## Deployment

### Azure App Service (Web API)

1. Publish the Web API project
2. Configure application settings in Azure portal
3. Set up managed identity for Azure services

### Azure Functions

1. Deploy using Azure Functions Core Tools or Visual Studio
2. Configure application settings
3. Set up storage account for queues and triggers

## Security Considerations

- Store sensitive configuration in Azure Key Vault
- Use managed identities for Azure service authentication
- Implement proper CORS policies for production
- Validate and sanitize all user inputs
- Implement rate limiting for API endpoints

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests for new functionality
5. Submit a pull request

## License

This project is licensed under the MIT License.

## Support

For issues and questions:
- Create an issue in the GitHub repository
- Check the logs in Azure Application Insights
- Review the health check endpoints