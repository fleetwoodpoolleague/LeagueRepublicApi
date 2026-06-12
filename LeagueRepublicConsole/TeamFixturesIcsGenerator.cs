using LeagueRepublicApi;
using LeagueRepublicApi.Models.Fixtures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LeagueRepublicConsole;

public sealed class TeamFixturesIcsGenerator
{
    private readonly ILogger<TeamFixturesIcsGenerator> _logger;
    private readonly IConfiguration _config;
    private readonly ILeagueRepublicApiClient _api;
    private readonly IFileWriter _files;

    public TeamFixturesIcsGenerator(ILogger<TeamFixturesIcsGenerator> logger, IConfiguration config, ILeagueRepublicApiClient api, IFileWriter fileWriter)
    {
        _logger = logger;
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _files = fileWriter ?? throw new ArgumentNullException(nameof(fileWriter));
    }

    public async Task RunAsync(string? leagueId, string leagueName, string teamName)
    {
        if (string.IsNullOrWhiteSpace(teamName))
            throw new ArgumentException("Team name must be provided", nameof(teamName));

        var leagueIdStr = string.IsNullOrEmpty(leagueId) ? _config["leagueid"] : leagueId;
        if (string.IsNullOrWhiteSpace(leagueIdStr))
            throw new InvalidOperationException("Missing 'leagueid' configuration value.");
        if (!long.TryParse(leagueIdStr, out var id))
            throw new InvalidOperationException("Invalid 'leagueid' configuration value.");

        _logger.LogDebug("Loading seasons for {LeagueId}", id);
        var seasons = await _api.GetSeasonsForLeagueAsync(id);
        var current = seasons.FirstOrDefault(s => s.CurrentSeason) ?? seasons.FirstOrDefault();
        if (current is null)
            throw new InvalidOperationException("No seasons returned for league.");

        var seasonId = current.SeasonId;
        var fixtures = await _api.GetFixturesForSeasonAsync(seasonId);

        // Filter fixtures where the given team participates (home or away)
        var teamFixtures = fixtures
            .Where(f => string.Equals(f.HomeTeamName, teamName, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(f.RoadTeamName, teamName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var ics = IcsBuilder.Build(leagueName, teamName, teamFixtures, DateTime.UtcNow);
        var fileName = IcsBuilder.MakeSafeFileName(teamName + ".ics");
        _files.WriteAllText(fileName, ics);
    }
}