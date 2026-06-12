using LeagueRepublicApi;
using LeagueRepublicApi.Models.Fixtures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LeagueRepublicConsole;

public sealed class FixturesIcsGenerator
{
    private readonly ILogger<FixturesIcsGenerator> _logger;
    private readonly IConfiguration _config;
    private readonly ILeagueRepublicApiClient _api;
    private readonly IFileWriter _files;

    public FixturesIcsGenerator(ILogger<FixturesIcsGenerator> logger, IConfiguration config, ILeagueRepublicApiClient api, IFileWriter files)
    {
        _logger = logger;
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _files = files ?? throw new ArgumentNullException(nameof(files));
    }

    public async Task RunAsync(string? leagueId, string? leagueName)
    {
        var leagueIdStr = string.IsNullOrEmpty(leagueId) ?  _config["leagueid"] : leagueId;
        
        if (string.IsNullOrWhiteSpace(leagueIdStr))
            throw new InvalidOperationException("Missing 'leagueid' configuration value.");

        if (!long.TryParse(leagueIdStr, out var id ))
            throw new InvalidOperationException("Invalid 'leagueid' configuration value.");

        _logger.LogDebug("Loading seasons for {LeagueId}", leagueId);
        var seasons = await _api.GetSeasonsForLeagueAsync(id);
        var current = seasons.FirstOrDefault(s => s.CurrentSeason) ?? seasons.FirstOrDefault();
        if (current is null)
            throw new InvalidOperationException("No seasons returned for league.");

        var seasonId = current.SeasonId;
        var groupsTask = _api.GetFixtureGroupsForSeasonAsync(seasonId);
        var fixturesTask = _api.GetFixturesForSeasonAsync(seasonId);
        await Task.WhenAll(groupsTask, fixturesTask);
        var groups = await groupsTask;
        var fixtures = await fixturesTask;

        // Only include groups that are divisions (FixtureTypeId == 1) if such info exists
        var divisionGroups = groups.Where(g => g.FixtureTypeId == 1).ToList();

        var fixturesByGroup = fixtures
            .Where(f => f.FixtureGroupIdentifier.HasValue)
            .GroupBy(f => f.FixtureGroupIdentifier!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var dtStamp = DateTime.UtcNow;
        foreach (var group in divisionGroups)
        {
            fixturesByGroup.TryGetValue(group.FixtureGroupIdentifier, out var groupFixtures);
            groupFixtures ??= new List<Fixture>();
            var ics = IcsBuilder.Build(leagueName, group.FixtureGroupDesc ?? $"Group {group.FixtureGroupIdentifier}", groupFixtures, dtStamp);
            var safeName = IcsBuilder.MakeSafeFileName((group.FixtureGroupDesc ?? group.FixtureGroupIdentifier.ToString()) + ".ics");
            _files.WriteAllText(safeName, ics);
        }
    }
}
