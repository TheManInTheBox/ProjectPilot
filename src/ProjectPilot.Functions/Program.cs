using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using ProjectPilot.Application.Extensions;
using ProjectPilot.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        
        // Add custom services
        services.AddInfrastructureServices(context.Configuration);
        services.AddApplicationServices();
    })
    .Build();

host.Run();