using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using ProjectPilot.Core.Interfaces;
using ProjectPilot.Infrastructure.Configuration;
using ProjectPilot.Infrastructure.Services;
using Microsoft.Azure.Cosmos;

namespace ProjectPilot.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure options
        services.Configure<AzureSpeechOptions>(configuration.GetSection("AzureSpeech"));
        services.Configure<AzureOpenAIOptions>(configuration.GetSection("AzureOpenAI"));
        services.Configure<GitHubOptions>(configuration.GetSection("GitHub"));
        services.Configure<CosmosDbOptions>(configuration.GetSection("CosmosDb"));

        // Register services
        services.AddTransient<ISpeechToTextService, AzureSpeechToTextService>();
        services.AddTransient<IOpenAIService, AzureOpenAIService>();
        services.AddTransient<IGitHubService, GitHubService>();

        // Register Cosmos DB client
        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<CosmosDbOptions>>().Value;
            return new CosmosClient(options.Endpoint, options.Key);
        });

        // Register repositories
        services.AddScoped(typeof(IRepository<>), typeof(CosmosDbRepository<>));

        return services;
    }
}