using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using ProjectPilot.Core.Interfaces;
using ProjectPilot.Infrastructure.Configuration;
using ProjectPilot.Infrastructure.Services;

namespace ProjectPilot.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure options
        services.Configure<AzureSpeechOptions>(configuration.GetSection("AzureSpeech"));
        services.Configure<AzureOpenAIOptions>(configuration.GetSection("AzureOpenAI"));
        services.Configure<GitHubOptions>(configuration.GetSection("GitHub"));

        // Register services
        services.AddTransient<ISpeechToTextService, AzureSpeechToTextService>();
        services.AddTransient<IOpenAIService, AzureOpenAIService>();
        services.AddTransient<IGitHubService, GitHubService>();

        return services;
    }
}