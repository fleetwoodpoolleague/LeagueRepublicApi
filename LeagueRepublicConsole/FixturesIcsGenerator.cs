using System.Globalization;
using System.Text;
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

    public async Task RunAsync(string? leagueId)
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
        var groups = await _api.GetFixtureGroupsForSeasonAsync(seasonId);
        var fixtures = await _api.GetFixturesForSeasonAsync(seasonId);

        // Only include groups that are divisions (FixtureTypeId == 1) if such info exists
        var divisionGroups = groups.Where(g => g.FixtureTypeId == 1).ToList();

        var fixturesByGroup = fixtures
            .Where(f => f.FixtureGroupIdentifier.HasValue)
            .GroupBy(f => f.FixtureGroupIdentifier!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var group in divisionGroups)
        {
            fixturesByGroup.TryGetValue(group.FixtureGroupIdentifier, out var groupFixtures);
            groupFixtures ??= new List<Fixture>();
            var ics = BuildIcs(group.FixtureGroupDesc ?? $"Group {group.FixtureGroupIdentifier}", groupFixtures);
            var safeName = MakeSafeFileName((group.FixtureGroupDesc ?? group.FixtureGroupIdentifier.ToString()) + ".ics");
            _files.WriteAllText(safeName, ics);
        }
    }

    private static string BuildIcs(string calendarName, List<Fixture> fixtures)
    {
        var sb = new StringBuilder();
        sb.Append("BEGIN:VCALENDAR\r\n");
        sb.Append("VERSION:2.0\r\n");
        sb.Append("PRODID:-//github.com/sgrassie/LeagueRepublicConsole//EN\r\n");
        sb.Append("X-WR-CALNAME:").Append(Escape(calendarName)).Append("\r\n");
        sb.Append("X-WR-TIMEZONE:Europe/London\r\n");
        sb.Append("CALSCALE:GREGORIAN\r\n");
        
        foreach (var f in fixtures.OrderBy(f => f.FixtureDateInMilliseconds ?? long.MaxValue))
        {
            sb.Append("BEGIN:VEVENT\r\n");
            var uid = $"{f.FixtureId}@leaguerepublic";
            sb.Append("UID:").Append(uid).Append("\r\n");
            var stamp = DateTime.UtcNow;
            sb.Append("DTSTAMP:").Append(FormatDateTimeUtc(stamp)).Append("\r\n");
            if (f.FixtureDateInMilliseconds is long ms)
            {
                var dt = DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime;
                sb.Append("DTSTART:").Append(FormatDateTimeUtc(dt)).Append("\r\n");
            }
            var summary = $"{f.HomeTeamName} vs {f.RoadTeamName}";
            sb.Append("SUMMARY:").Append(Escape(summary)).Append("\r\n");
            if (!string.IsNullOrWhiteSpace(f.VenueAndSubVenueDesc))
            {
                sb.Append("LOCATION:").Append(Escape(f.VenueAndSubVenueDesc!)).Append("\r\n");
            }
            var desc = BuildDescription(f);
            if (!string.IsNullOrEmpty(desc))
            {
                sb.Append("DESCRIPTION:").Append(Escape(desc)).Append("\r\n");
            }
            sb.Append("END:VEVENT\r\n");
        }
        sb.Append("END:VCALENDAR\r\n");
        return sb.ToString();
    }

    private static string BuildDescription(Fixture f)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(f.FixtureNote)) parts.Add(f.FixtureNote!);
        if (!string.IsNullOrWhiteSpace(f.FixtureStatusDesc)) parts.Add($"Status: {f.FixtureStatusDesc}");
        if (f.Result)
        {
            var score = $"Result: {f.HomeScore ?? ""}-{f.RoadScore ?? ""}".Trim();
            parts.Add(score);
        }
        return string.Join("; ", parts);
    }

    private static string FormatDateTimeUtc(DateTime dt)
        => dt.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);

    private static string Escape(string value)
    {
        // Basic ICS escaping: backslash, comma, semicolon, and newlines
        return value
            .Replace("\\", "\\\\")
            .Replace(";", "\\;")
            .Replace(",", "\\,")
            .Replace("\r\n", "\\n")
            .Replace("\n", "\\n");
    }

    private static string MakeSafeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        
        return name.Replace(" ", "-");
    }
}
