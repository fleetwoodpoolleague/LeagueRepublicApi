using LeagueRepublicApi;
using LeagueRepublicConsole;
using LeagueRepublicConsole.Commands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TimeWarp.Mediator;
using TimeWarp.Nuru;

var builder = new NuruAppBuilder()
    .AddDependencyInjection()
    .ConfigureServices(services =>
    {
        var config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
        services.AddHttpClient<ILeagueRepublicApiClient, LeagueRepublicApiClient>();
        services.AddSingleton<IConfiguration>(config);
        services.AddSingleton<IFileWriter, PhysicalFileWriter>();
        services.AddSingleton<LeagueRepublicClientOptions>();
        services.AddTransient<FixturesIcsGenerator>();

        services.AddTransient<IRequestHandler<IcsCommand>, IcsCommand.Handler>();
    })
    .AddRoute<IcsCommand>(
        pattern: "ics",
        description: "Generate ics files for the given league. Assumes a leagueid is configured in user secrets."
    )
    .AddRoute<IcsCommand>(
        pattern: "ics {leagueid}",
        description: "Generate ics files for the given leagueid."
    )
    ;

NuruApp app = builder.Build();

// Run the app with the current command line arguments
return await app.RunAsync(args);