using LeagueRepublicApi;
using LeagueRepublicConsole;
using LeagueRepublicConsole.Commands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Extensions.Logging;
using TimeWarp.Mediator;
using TimeWarp.Nuru;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .CreateLogger();

Log.Logger.Information("Initialising League Republic Console...");

var builder = new NuruAppBuilder()
        .UseLogging(new SerilogLoggerFactory(Log.Logger))
        .AddDependencyInjection(config => config.RegisterServicesFromAssemblyContaining<Program>())
        .ConfigureServices(services =>
        {
            var config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
            services.AddHttpClient<ILeagueRepublicApiClient, LeagueRepublicApiClient>();
            services.AddSingleton<IConfiguration>(config);
            services.AddSingleton<IFileWriter, PhysicalFileWriter>();
            services.AddSingleton<LeagueRepublicClientOptions>();
            services.AddTransient<FixturesIcsGenerator>();
            services.AddTransient<TeamFixturesIcsGenerator>();
        })
        .Map<IcsCommand>(
            pattern: "ics {leagueid?} --league-name {leaguename}",
            description: "Generate an ics file for the given league."
        )
        .Map<TeamIcsCommand>(
            pattern: "ics team {leagueid?} --league-name {leaguename} --team-name {teamname}",
            description: "Generate an ics file for the given division and team."
        );

NuruApp app = builder.Build();

// Run the app with the current command line arguments
return await app.RunAsync(args);