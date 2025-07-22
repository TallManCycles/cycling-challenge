// ABOUTME: This is the entry point for the Azure Functions application
// ABOUTME: Configures the host with Entity Framework and HTTP extensions
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CyclingChallenge.Data;
using CyclingChallenge.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddDbContext<ChallengeDbContext>(options =>
        {
            var connectionString = Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING") 
                ?? "Data Source=challenges.db";
            options.UseSqlite(connectionString);
        });
        
        services.AddHttpClient();
        
        services.AddScoped<GarminAuthService>();
        services.AddScoped<ChallengeService>();
        services.AddScoped<ActivityProcessingService>();
    })
    .Build();

host.Run();