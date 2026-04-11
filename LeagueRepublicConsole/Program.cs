using LeagueRepublicApi;
using LeagueRepublicConsole;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Extensions.Logging;
using TimeWarp.Nuru;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .CreateLogger();

Log.Logger.Information("Initialising League Republic Console...");

var builder = NuruApp.CreateBuilder()
    .UseLogging(new SerilogLoggerFactory(Log.Logger))
    .DiscoverEndpoints()
    .ConfigureServices(services =>
    {
        var config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
        services.AddHttpClient<ILeagueRepublicApiClient, LeagueRepublicApiClient>();
        services.AddSingleton<IConfiguration>(config);
        services.AddSingleton<IFileWriter, PhysicalFileWriter>();
        services.AddSingleton<LeagueRepublicClientOptions>();
        services.AddTransient<FixturesIcsGenerator>();
        services.AddTransient<TeamFixturesIcsGenerator>();
    });

NuruApp app = builder.Build();

// Run the app with the current command line arguments
return await app.RunAsync(args);