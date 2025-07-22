// ABOUTME: This is the entry point for the Azure Functions application
// ABOUTME: Configures the host with Entity Framework and HTTP extensions
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using CyclingChallenge.Data;
using CyclingChallenge.Services;
using Serilog;
using Serilog.Events;

// Configure Serilog for file logging
var backendDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!;
var projectRoot = Directory.GetParent(backendDirectory)?.Parent?.Parent?.FullName ?? backendDirectory;
var logDirectory = Path.Combine(projectRoot, "backend", "logs");
Directory.CreateDirectory(logDirectory);

// Debug: Print the log directory path
Console.WriteLine($"=== LOG DIRECTORY DEBUG ===");
Console.WriteLine($"Current Directory: {Directory.GetCurrentDirectory()}");
Console.WriteLine($"Log Directory: {logDirectory}");
Console.WriteLine($"Directory Exists: {Directory.Exists(logDirectory)}");

var logFilePath = Path.Combine(logDirectory, "cycling-challenge-.txt");
Console.WriteLine($"Log File Path: {logFilePath}");

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {NewLine}{Exception}")
    .WriteTo.File(
        path: logFilePath,
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj} {NewLine}{Exception}",
        flushToDiskInterval: TimeSpan.FromSeconds(1))
    .CreateLogger();

Console.WriteLine($"Serilog configured. Testing file write...");

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddSerilog();
    })
    .ConfigureServices(services =>
    {
        services.AddDbContext<ChallengeDbContext>(options =>
        {
            var connectionString = Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING");
            if (connectionString == null)
            {
                // Get the project root directory and create Data folder path
                var projectRoot = Directory.GetParent(Directory.GetCurrentDirectory())?.Parent?.Parent?.FullName 
                    ?? Directory.GetCurrentDirectory();
                var dataDir = Path.Combine(projectRoot, "Data");
                Directory.CreateDirectory(dataDir);
                connectionString = $"Data Source={Path.Combine(dataDir, "challenges.db")}";
            }
            options.UseSqlite(connectionString);
        });
        
        services.AddHttpClient();
        
        // services.AddScoped<GarminAuthService>(); // Disabled - using OAuth 1.0 instead
        services.AddScoped<GarminOAuth1Service>();
        services.AddScoped<ChallengeService>();
        services.AddScoped<ActivityProcessingService>();
    })
    .Build();

try
{
    Log.Information("=== TESTING FILE LOGGING ===");
    Log.Information("Starting Cycling Challenge Azure Functions Host");
    Log.Information("Current Directory: {CurrentDir}", Directory.GetCurrentDirectory());
    Log.Information("Log Directory: {LogDir}", logDirectory);
    
    // Ensure database is created
    Log.Information("Ensuring database is created...");
    using (var scope = host.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<ChallengeDbContext>();
        context.Database.EnsureCreated();
    
        // Log the location of the database.
        var connection = context.Database.GetDbConnection();
        var connectionStringBuilder = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(connection.ConnectionString);
        var dbPath = Path.GetFullPath(connectionStringBuilder.DataSource);
        Log.Information("Database path: {DbPath}", dbPath);
        
    
        Log.Information("Database creation completed.");
    }
    
    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}