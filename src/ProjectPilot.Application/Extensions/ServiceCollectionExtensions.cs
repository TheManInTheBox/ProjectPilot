using Microsoft.Extensions.DependencyInjection;
using ProjectPilot.Application.Services;
using ProjectPilot.Core.Interfaces;

namespace ProjectPilot.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register orchestration services
        services.AddScoped<ITranscriptionService, TranscriptionOrchestrationService>();
        services.AddScoped<IGitHubIntegrationService, GitHubIntegrationOrchestrationService>();
        services.AddScoped<ITaskExtractionService, TaskExtractionOrchestrationService>();

        return services;
    }
}