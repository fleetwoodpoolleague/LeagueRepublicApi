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
using LeagueRepublicConsole;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LeagueRepublicConsole.Tests;

public class FixturesIcsGeneratorTests
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
    public async Task Loads_LeagueId_From_Config_And_Calls_Api()
    {
        var fakeApi = new FakeApiClient();
        var files = new InMemoryFileWriter();
        var config = ConfigWithLeagueId(123);
        var gen = new FixturesIcsGenerator(NullLogger<FixturesIcsGenerator>.Instance, config, fakeApi, files);

        fakeApi.Seasons.Add(new Season { SeasonId = 1, SeasonName = "2024/25", CurrentSeason = true });
        fakeApi.Groups.Add(new FixtureGroup { FixtureGroupIdentifier = 10, FixtureGroupDesc = "Division A", FixtureTypeId = 1, FixtureTypeDesc = "Division" });
        fakeApi.Fixtures.Add(new Fixture { FixtureId = 100, FixtureGroupIdentifier = 10, HomeTeamName = "Team A", RoadTeamName = "Team B", FixtureDateInMilliseconds = 1730000000000, VenueAndSubVenueDesc = "Main Hall" });

        await gen.RunAsync("123");

        fakeApi.LeagueIdCalls.Should().ContainSingle().Which.Should().Be(123);
        fakeApi.GroupCalls.Should().ContainSingle().Which.Should().Be(1);
        fakeApi.FixtureSeasonCalls.Should().ContainSingle().Which.Should().Be(1);

        files.Files.Keys.Should().ContainSingle(k => k.EndsWith("Division-A.ics"));
        var content = files.Files.Single().Value;
        content.Should().Contain("BEGIN:VCALENDAR");
        content.Should().Contain("BEGIN:VEVENT");
        content.Should().Contain("SUMMARY:Team A vs Team B");
    }

    [Fact]
    public async Task Groups_Fixtures_By_Division_And_Writes_One_File_Per_Group()
    {
        var fakeApi = new FakeApiClient();
        var files = new InMemoryFileWriter();
        var config = ConfigWithLeagueId(123);
        var gen = new FixturesIcsGenerator(NullLogger<FixturesIcsGenerator>.Instance, config, fakeApi, files);

        fakeApi.Seasons.Add(new Season { SeasonId = 1, SeasonName = "2024/25", CurrentSeason = true });
        fakeApi.Groups.AddRange(new[]
        {
            new FixtureGroup { FixtureGroupIdentifier = 10, FixtureGroupDesc = "Division A", FixtureTypeId = 1, FixtureTypeDesc = "Division" },
            new FixtureGroup { FixtureGroupIdentifier = 11, FixtureGroupDesc = "Division B", FixtureTypeId = 1, FixtureTypeDesc = "Division" }
        });
        fakeApi.Fixtures.AddRange(new[]
        {
            new Fixture { FixtureId = 100, FixtureGroupIdentifier = 10, HomeTeamName = "A1", RoadTeamName = "A2", FixtureDateInMilliseconds = 1730000000000 },
            new Fixture { FixtureId = 101, FixtureGroupIdentifier = 11, HomeTeamName = "B1", RoadTeamName = "B2", FixtureDateInMilliseconds = 1730003600000 }
        });

        await gen.RunAsync("123");

        files.Files.Keys.Count(k => k.EndsWith(".ics")).Should().Be(2);
        files.Files.Keys.Should().Contain(k => k.EndsWith("Division-A.ics"));
        files.Files.Keys.Should().Contain(k => k.EndsWith("Division-B.ics"));
    }

    [Fact]
    public async Task Serializes_Valid_Ics_Content_For_Fixtures()
    {
        var fakeApi = new FakeApiClient();
        var files = new InMemoryFileWriter();
        var config = ConfigWithLeagueId(123);
        var gen = new FixturesIcsGenerator(NullLogger<FixturesIcsGenerator>.Instance, config, fakeApi, files);

        fakeApi.Seasons.Add(new Season { SeasonId = 1, SeasonName = "2024/25", CurrentSeason = true });
        fakeApi.Groups.Add(new FixtureGroup { FixtureGroupIdentifier = 10, FixtureGroupDesc = "Division-A", FixtureTypeId = 1, FixtureTypeDesc = "Division" });
        fakeApi.Fixtures.Add(new Fixture { FixtureId = 100, FixtureGroupIdentifier = 10, HomeTeamName = "Team A", RoadTeamName = "Team B", FixtureDateInMilliseconds = 1730000000000, VenueAndSubVenueDesc = "Main Hall" });

        await gen.RunAsync("123");

        var ics = files.Files.Single().Value;
        ics.Should().StartWith("BEGIN:VCALENDAR\r\nVERSION:2.0\r\nPRODID:-//github.com/sgrassie/LeagueRepublicConsole//EN\r\n");
        ics.Should().Contain("BEGIN:VEVENT\r\n");
        ics.Should().Contain("SUMMARY:Team A vs Team B\r\n");
        ics.Should().Contain("UID:100@leaguerepublic\r\n");
        ics.Should().Contain("DTSTAMP:");
        ics.Should().EndWith("END:VCALENDAR\r\n");
    }
}
