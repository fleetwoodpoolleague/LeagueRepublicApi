using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using LeagueRepublicApi;
using LeagueRepublicApi.Models.FixtureGroups;
using LeagueRepublicApi.Models.Fixtures;
using LeagueRepublicApi.Models.Seasons;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LeagueRepublicConsole.Tests;

public class TeamFixturesIcsGeneratorTests
{
    private sealed class FakeApiClient : ILeagueRepublicApiClient
    {
        public List<Season> Seasons { get; } = new();
        public List<FixtureGroup> Groups { get; } = new();
        public List<Fixture> Fixtures { get; } = new();

        public Task<IReadOnlyList<Season>> GetSeasonsForLeagueAsync(long? leagueId = null, CancellationToken cancellationToken = default)
        {
            LeagueIdCalls.Add(leagueId);
            return Task.FromResult((IReadOnlyList<Season>)Seasons);
        }

        public Task<IReadOnlyList<FixtureGroup>> GetFixtureGroupsForSeasonAsync(long seasonId, CancellationToken cancellationToken = default)
        {
            GroupCalls.Add(seasonId);
            return Task.FromResult((IReadOnlyList<FixtureGroup>)Groups);
        }

        public Task<IReadOnlyList<Fixture>> GetFixturesForSeasonAsync(long seasonId, CancellationToken cancellationToken = default)
        {
            FixtureSeasonCalls.Add(seasonId);
            return Task.FromResult((IReadOnlyList<Fixture>)Fixtures);
        }

        public Task<IReadOnlyList<Fixture>> GetFixturesForFixtureGroupAsync(long fixtureGroupIdentifier, CancellationToken cancellationToken = default)
        {
            FixtureGroupCalls.Add(fixtureGroupIdentifier);
            return Task.FromResult((IReadOnlyList<Fixture>)Fixtures.Where(f => f.FixtureGroupIdentifier == fixtureGroupIdentifier).ToList());
        }

        public List<long?> LeagueIdCalls { get; } = new();
        public List<long> GroupCalls { get; } = new();
        public List<long> FixtureSeasonCalls { get; } = new();
        public List<long> FixtureGroupCalls { get; } = new();
    }

    private sealed class InMemoryFileWriter : IFileWriter
    {
        public Dictionary<string, string> Files { get; } = new();
        public void WriteAllText(string path, string contents) => Files[path] = contents;
    }

    private static IConfiguration ConfigWithLeagueId(long id)
    {
        var dict = new Dictionary<string, string?> { ["leagueid"] = id.ToString() };
        return new ConfigurationBuilder().AddInMemoryCollection(dict!).Build();
    }

    [Fact]
    public async Task Generates_Single_Team_Ics_File_Filtering_Fixtures()
    {
        var api = new FakeApiClient();
        var files = new InMemoryFileWriter();
        var cfg = ConfigWithLeagueId(77);
        var gen = new TeamFixturesIcsGenerator(NullLogger<TeamFixturesIcsGenerator>.Instance, cfg, api, files);

        api.Seasons.Add(new Season { SeasonId = 1, CurrentSeason = true, SeasonName = "2024/25" });
        api.Fixtures.AddRange(new[]
        {
            new Fixture { FixtureId = 1, FixtureGroupIdentifier = 10, HomeTeamName = "Alpha", RoadTeamName = "Beta", FixtureDateInMilliseconds = 1730000000000 },
            new Fixture { FixtureId = 2, FixtureGroupIdentifier = 10, HomeTeamName = "Gamma", RoadTeamName = "Alpha", FixtureDateInMilliseconds = 1730003600000 },
            new Fixture { FixtureId = 3, FixtureGroupIdentifier = 11, HomeTeamName = "Delta", RoadTeamName = "Epsilon", FixtureDateInMilliseconds = 1730007200000 }
        });

        await gen.RunAsync("77", "League X", "Alpha");

        files.Files.Keys.Should().ContainSingle(k => k.EndsWith("Alpha.ics") || k.EndsWith("Alpha-ics"));
        var ics = files.Files.Single().Value;
        ics.Should().Contain("BEGIN:VCALENDAR");
        ics.Should().Contain("BEGIN:VEVENT");
        ics.Should().Contain("SUMMARY:Alpha vs Beta");
        ics.Should().Contain("SUMMARY:Gamma vs Alpha");
        ics.Should().NotContain("Delta vs Epsilon");
    }

    [Fact]
    public async Task Escapes_Team_Name_In_Filename_And_Title()
    {
        var api = new FakeApiClient();
        var files = new InMemoryFileWriter();
        var cfg = ConfigWithLeagueId(77);
        var gen = new TeamFixturesIcsGenerator(NullLogger<TeamFixturesIcsGenerator>.Instance, cfg, api, files);

        api.Seasons.Add(new Season { SeasonId = 1, CurrentSeason = true });
        api.Fixtures.Add(new Fixture { FixtureId = 10, FixtureGroupIdentifier = 10, HomeTeamName = "AC/DC Stars", RoadTeamName = "Beta", FixtureDateInMilliseconds = 1730000000000 });

        await gen.RunAsync("77", "League X", "AC/DC Stars");

        files.Files.Keys.Should().ContainSingle(k => k.EndsWith("AC_DC-Stars.ics"));
        var ics = files.Files.Single().Value;
        ics.Should().Contain("X-WR-CALNAME:League X: AC\\/DC Stars");
    }
}
