using System;
using System.Net.Http;
using System.Threading.Tasks;
using LeagueRepublicApi;
using LeagueRepublicIcs;
using Microsoft.Extensions.Configuration;

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
    Console.Error.WriteLine($"LeagueRepublicIcs: {ex.Message}");
    Console.Error.WriteLine(ex.ToString());
    return 1;
}