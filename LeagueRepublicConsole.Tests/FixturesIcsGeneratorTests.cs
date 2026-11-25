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
            ReceivedTokens.Add(cancellationToken);
            return Task.FromResult((IReadOnlyList<Season>)Seasons);
        }

        public Task<IReadOnlyList<FixtureGroup>> GetFixtureGroupsForSeasonAsync(long seasonId, CancellationToken cancellationToken = default)
        {
            GroupCalls.Add(seasonId);
            ReceivedTokens.Add(cancellationToken);
            return Task.FromResult((IReadOnlyList<FixtureGroup>)Groups);
        }

        public Task<IReadOnlyList<Fixture>> GetFixturesForSeasonAsync(long seasonId, CancellationToken cancellationToken = default)
        {
            FixtureSeasonCalls.Add(seasonId);
            ReceivedTokens.Add(cancellationToken);
            return Task.FromResult((IReadOnlyList<Fixture>)Fixtures);
        }

        public List<CancellationToken> ReceivedTokens { get; } = new();

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
        public string ReadAllText(string path) => Files[path];
        public IEnumerable<string> GetFiles(string directory, string searchPattern)
            => Files.Keys.Where(k => k.StartsWith(directory));
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

        await gen.RunAsync("123", "Test League");

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

        await gen.RunAsync("123", "Test League");

        files.Files.Keys.Count(k => k.EndsWith(".ics")).Should().Be(2);
        files.Files.Keys.Should().Contain(k => k.EndsWith("Division-A.ics"));
        files.Files.Keys.Should().Contain(k => k.EndsWith("Division-B.ics"));
    }

    [Fact]
    public async Task Falls_Back_To_First_Season_When_None_Marked_Current()
    {
        var fakeApi = new FakeApiClient();
        var files = new InMemoryFileWriter();
        var gen = new FixturesIcsGenerator(NullLogger<FixturesIcsGenerator>.Instance, ConfigWithLeagueId(123), fakeApi, files);
        fakeApi.Seasons.AddRange(new[]
        {
            new Season { SeasonId = 7, SeasonName = "2023/24", CurrentSeason = false },
            new Season { SeasonId = 8, SeasonName = "2024/25", CurrentSeason = false }
        });

        await gen.RunAsync("123", "Test League");

        fakeApi.GroupCalls.Should().ContainSingle().Which.Should().Be(7);
    }

    [Fact]
    public async Task Throws_When_No_Seasons_Returned()
    {
        var fakeApi = new FakeApiClient();
        var files = new InMemoryFileWriter();
        var gen = new FixturesIcsGenerator(NullLogger<FixturesIcsGenerator>.Instance, ConfigWithLeagueId(123), fakeApi, files);

        var act = async () => await gen.RunAsync("123", "Test League");

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*No seasons*");
    }

    [Fact]
    public async Task Throws_When_LeagueId_Missing_From_Arg_And_Config()
    {
        var fakeApi = new FakeApiClient();
        var files = new InMemoryFileWriter();
        var emptyConfig = new ConfigurationBuilder().Build();
        var gen = new FixturesIcsGenerator(NullLogger<FixturesIcsGenerator>.Instance, emptyConfig, fakeApi, files);

        var act = async () => await gen.RunAsync(null, "Test League");

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Missing*leagueid*");
    }

    [Fact]
    public async Task Throws_When_LeagueId_Is_Not_Numeric()
    {
        var fakeApi = new FakeApiClient();
        var files = new InMemoryFileWriter();
        var gen = new FixturesIcsGenerator(NullLogger<FixturesIcsGenerator>.Instance, ConfigWithLeagueId(123), fakeApi, files);

        var act = async () => await gen.RunAsync("not-a-number", "Test League");

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Invalid*leagueid*");
    }

    [Fact]
    public async Task Resolves_LeagueId_From_Config_When_Arg_Is_Empty()
    {
        var fakeApi = new FakeApiClient();
        var files = new InMemoryFileWriter();
        var gen = new FixturesIcsGenerator(NullLogger<FixturesIcsGenerator>.Instance, ConfigWithLeagueId(456), fakeApi, files);
        fakeApi.Seasons.Add(new Season { SeasonId = 1, CurrentSeason = true });

        await gen.RunAsync(string.Empty, "Test League");

        fakeApi.LeagueIdCalls.Should().ContainSingle().Which.Should().Be(456);
    }

    [Fact]
    public async Task Excludes_Non_Division_Fixture_Groups()
    {
        var fakeApi = new FakeApiClient();
        var files = new InMemoryFileWriter();
        var gen = new FixturesIcsGenerator(NullLogger<FixturesIcsGenerator>.Instance, ConfigWithLeagueId(123), fakeApi, files);
        fakeApi.Seasons.Add(new Season { SeasonId = 1, CurrentSeason = true });
        fakeApi.Groups.AddRange(new[]
        {
            new FixtureGroup { FixtureGroupIdentifier = 10, FixtureGroupDesc = "Division A", FixtureTypeId = 1, FixtureTypeDesc = "Division" },
            new FixtureGroup { FixtureGroupIdentifier = 20, FixtureGroupDesc = "Knockout Cup", FixtureTypeId = 2, FixtureTypeDesc = "Cup" }
        });

        await gen.RunAsync("123", "Test League");

        files.Files.Keys.Should().ContainSingle().Which.Should().EndWith("Division-A.ics");
    }

    [Fact]
    public async Task Writes_Empty_Calendar_For_Division_With_No_Fixtures()
    {
        var fakeApi = new FakeApiClient();
        var files = new InMemoryFileWriter();
        var gen = new FixturesIcsGenerator(NullLogger<FixturesIcsGenerator>.Instance, ConfigWithLeagueId(123), fakeApi, files);
        fakeApi.Seasons.Add(new Season { SeasonId = 1, CurrentSeason = true });
        fakeApi.Groups.Add(new FixtureGroup { FixtureGroupIdentifier = 10, FixtureGroupDesc = "Division A", FixtureTypeId = 1, FixtureTypeDesc = "Division" });

        await gen.RunAsync("123", "Test League");

        var ics = files.Files["Division-A.ics"];
        ics.Should().StartWith("BEGIN:VCALENDAR").And.EndWith("END:VCALENDAR\r\n").And.NotContain("BEGIN:VEVENT");
    }

    [Fact]
    public async Task Excludes_Fixtures_Without_Group_Identifier()
    {
        var fakeApi = new FakeApiClient();
        var files = new InMemoryFileWriter();
        var gen = new FixturesIcsGenerator(NullLogger<FixturesIcsGenerator>.Instance, ConfigWithLeagueId(123), fakeApi, files);
        fakeApi.Seasons.Add(new Season { SeasonId = 1, CurrentSeason = true });
        fakeApi.Groups.Add(new FixtureGroup { FixtureGroupIdentifier = 10, FixtureGroupDesc = "Division A", FixtureTypeId = 1, FixtureTypeDesc = "Division" });
        fakeApi.Fixtures.Add(new Fixture { FixtureId = 100, FixtureGroupIdentifier = null, HomeTeamName = "Orphan A", RoadTeamName = "Orphan B", FixtureDateInMilliseconds = 1730000000000 });

        await gen.RunAsync("123", "Test League");

        files.Files["Division-A.ics"].Should().NotContain("Orphan A");
    }

    [Fact]
    public async Task Forwards_CancellationToken_To_Api_Calls()
    {
        var fakeApi = new FakeApiClient();
        var files = new InMemoryFileWriter();
        var config = ConfigWithLeagueId(123);
        var gen = new FixturesIcsGenerator(NullLogger<FixturesIcsGenerator>.Instance, config, fakeApi, files);
        fakeApi.Seasons.Add(new Season { SeasonId = 1, CurrentSeason = true });

        using var cts = new CancellationTokenSource();
        await gen.RunAsync("123", "Test League", cts.Token);

        fakeApi.ReceivedTokens.Should().HaveCount(3).And.AllSatisfy(t => t.Should().Be(cts.Token));
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

        await gen.RunAsync("123", "Test League");

        var ics = files.Files.Single().Value;
        ics.Should().StartWith("BEGIN:VCALENDAR\r\nVERSION:2.0\r\nPRODID:-//github.com/sgrassie/LeagueRepublicConsole//EN\r\n");
        ics.Should().Contain("BEGIN:VEVENT\r\n");
        ics.Should().Contain("SUMMARY:Team A vs Team B\r\n");
        ics.Should().Contain("UID:100@leaguerepublic\r\n");
        ics.Should().Contain("DTSTAMP:");
        ics.Should().EndWith("END:VCALENDAR\r\n");
    }

    [Fact]
    public async Task Writes_Manifest_Json_With_Divisions_And_Teams_When_Requested()
    {
        var fakeApi = new FakeApiClient();
        var files = new InMemoryFileWriter();
        var config = ConfigWithLeagueId(123);
        var gen = new FixturesIcsGenerator(NullLogger<FixturesIcsGenerator>.Instance, config, fakeApi, files);

        fakeApi.Seasons.Add(new Season { SeasonId = 1, SeasonName = "Summer 2026", CurrentSeason = true });
        fakeApi.Groups.Add(new FixtureGroup { FixtureGroupIdentifier = 10, FixtureGroupDesc = "Division 1", FixtureTypeId = 1, FixtureTypeDesc = "Division" });
        fakeApi.Fixtures.AddRange(new[]
        {
            new Fixture { FixtureId = 100, FixtureGroupIdentifier = 10, HomeTeamName = "Rossall Tavern ", RoadTeamName = "Bar 1", FixtureDateInMilliseconds = 1730000000000 },
            new Fixture { FixtureId = 101, FixtureGroupIdentifier = 20, HomeTeamName = "Bar 1", RoadTeamName = "Cup Opponent", FixtureDateInMilliseconds = 1730003600000 }
        });

        await gen.RunAsync("123", "Test League", "manifest.json");

        files.Files.Keys.Should().Contain("manifest.json");
        var manifest = files.Files["manifest.json"];
        manifest.Should().Contain("\"seasonName\": \"Summer 2026\"");
        manifest.Should().Contain("\"file\": \"Division-1.ics\"");
        manifest.Should().Contain("\"file\": \"Rossall-Tavern.ics\"");
        manifest.Should().Contain("\"file\": \"Bar-1.ics\"");
        manifest.Should().Contain("\"fixtureCount\": 2"); // Bar 1 has 1 division + 1 cup fixture
    }
}
