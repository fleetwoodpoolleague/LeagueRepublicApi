using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using LeagueRepublicApi;
using LeagueRepublicConsole;
using Microsoft.Extensions.Configuration;
using TimeWarp.Nuru;

static async Task<int> RunGeneratorAsync(long? leagueId)
{
    // Build configuration: user secrets + optional in-memory override from CLI
    var config = new ConfigurationBuilder()
        .AddUserSecrets<Program>()
        .Build();

    try
    {
        var http = new HttpClient();
        var api = new LeagueRepublicApiClient(http);
        var files = new PhysicalFileWriter();
        var generator = new FixturesIcsGenerator(config, api, files);
        await generator.RunAsync();
        return 0;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"LeagueRepublicConsole: {ex.Message}");
        Console.Error.WriteLine(ex.ToString());
        return 1;
    }
}

var builder = new NuruAppBuilder()
    .AddDependencyInjection()
    // `ics` with optional positional argument leagueid
    .AddRoute("ics {leagueid}", async (long leagueid) =>
    {
        await RunGeneratorAsync(leagueid);
    })
    .AddRoute("ics", async () =>
    {
        await RunGeneratorAsync(null);
    });

NuruApp app = builder.Build();

// Run the app with the current command line arguments
return await app.RunAsync(args);