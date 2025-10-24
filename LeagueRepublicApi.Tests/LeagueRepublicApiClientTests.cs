using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using LeagueRepublicApi.Tests.Fakes;
using Xunit;

namespace LeagueRepublicApi.Tests;

public class LeagueRepublicApiClientTests
{
    [Fact]
    public async Task GetSeasonsForLeague_UsesLeagueIdFromOptions_WhenNotProvided()
    {
        // Arrange
        const long leagueId = 770829510;
        var handler = new FakeHttpMessageHandler(req =>
        {
            req.RequestUri!.ToString().Should().Contain($"json/getSeasonsForLeague/{leagueId}.json");
            var json = "[ { \"currentSeason\": true, \"seasonEndDate\": \"20230617 23:59\", \"seasonEndDateInMilliseconds\": 1687046340000, \"seasonID\": 153020000, \"seasonName\": \"2022 / 2023\", \"seasonStartDate\": \"20220617 00:00\", \"seasonStartDateInMilliseconds\": 1655424000000 } ]";
            return FakeHttpMessageHandler.Json(json);
        });
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.leaguerepublic.com/") };
        var client = new LeagueRepublicApiClient(http, new LeagueRepublicClientOptions { LeagueId = leagueId });

        // Act
        var seasons = await client.GetSeasonsForLeagueAsync();

        // Assert
        seasons.Should().HaveCount(1);
        var s = seasons.Single();
        s.CurrentSeason.Should().BeTrue();
        s.SeasonId.Should().Be(153020000);
        s.SeasonName.Should().Be("2022 / 2023");
    }

    [Fact]
    public async Task GetSeasonsForLeague_Throws_WhenLeagueIdMissing()
    {
        var handler = new FakeHttpMessageHandler(_ => throw new Exception("Should not be called"));
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.leaguerepublic.com/") };
        var client = new LeagueRepublicApiClient(http);

        Func<Task> act = async () => await client.GetSeasonsForLeagueAsync();
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("A leagueId must be provided either in options or as a parameter.");
    }

    [Fact]
    public async Task GetFixtureGroupsForSeason_DeserializesResponse()
    {
        // Arrange
        const long seasonId = 153020000;
        var handler = new FakeHttpMessageHandler(req =>
        {
            req.RequestUri!.ToString().Should().Contain($"json/getFixtureGroupsForSeason/{seasonId}.json");
            var json = "[ { \"fixtureGroupDesc\": \"League 1\", \"fixtureGroupIdentifier\": 985736993, \"fixtureTypeDesc\": \"Division\", \"fixtureTypeID\": 1 } ]";
            return FakeHttpMessageHandler.Json(json);
        });
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.leaguerepublic.com/") };
        var client = new LeagueRepublicApiClient(http);

        // Act
        var groups = await client.GetFixtureGroupsForSeasonAsync(seasonId);

        // Assert
        groups.Should().ContainSingle();
        var g = groups.Single();
        g.FixtureGroupDesc.Should().Be("League 1");
        g.FixtureGroupIdentifier.Should().Be(985736993);
        g.FixtureTypeDesc.Should().Be("Division");
        g.FixtureTypeId.Should().Be(1);
    }

    [Fact]
    public async Task GetFixturesForSeason_DeserializesResponse()
    {
        // Arrange
        const long seasonId = 153020000;
        var handler = new FakeHttpMessageHandler(req =>
        {
            req.RequestUri!.ToString().Should().Contain($"json/getFixturesForSeason/{seasonId}.json");
            var json = "[ { \"fixtureID\": 32609296, \"fixtureGroupIdentifier\": 520732801, \"homeTeamName\": \"Bear Inn\", \"roadTeamName\": \"Waggon and Horses\", \"homeScore\": \"2\", \"roadScore\": \"1\", \"result\": true } ]";
            return FakeHttpMessageHandler.Json(json);
        });
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.leaguerepublic.com/") };
        var client = new LeagueRepublicApiClient(http);

        // Act
        var fixtures = await client.GetFixturesForSeasonAsync(seasonId);

        // Assert
        fixtures.Should().ContainSingle();
        var f = fixtures.Single();
        f.FixtureId.Should().Be(32609296);
        f.FixtureGroupIdentifier.Should().Be(520732801);
        f.HomeTeamName.Should().Be("Bear Inn");
        f.RoadTeamName.Should().Be("Waggon and Horses");
        f.HomeScore.Should().Be("2");
        f.RoadScore.Should().Be("1");
        f.Result.Should().BeTrue();
    }

    [Fact]
    public async Task GetFixturesForFixtureGroup_DeserializesResponse()
    {
        // Arrange
        const long fixtureGroupIdentifier = 520732801;
        var handler = new FakeHttpMessageHandler(req =>
        {
            req.RequestUri!.ToString().Should().Contain($"json/getFixturesForFixtureGroup/{fixtureGroupIdentifier}.json");
            var json = "[ { \"fixtureID\": 32609297, \"fixtureGroupIdentifier\": 520732801, \"homeTeamName\": \"Tap & Barrel\", \"roadTeamName\": \"The Clinton Arms\", \"homeScore\": \"2\", \"roadScore\": \"1\", \"result\": true } ]";
            return FakeHttpMessageHandler.Json(json);
        });
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.leaguerepublic.com/") };
        var client = new LeagueRepublicApiClient(http);

        // Act
        var fixtures = await client.GetFixturesForFixtureGroupAsync(fixtureGroupIdentifier);

        // Assert
        fixtures.Should().ContainSingle();
        var f = fixtures.Single();
        f.FixtureId.Should().Be(32609297);
        f.FixtureGroupIdentifier.Should().Be(520732801);
        f.HomeTeamName.Should().Be("Tap & Barrel");
        f.RoadTeamName.Should().Be("The Clinton Arms");
        f.HomeScore.Should().Be("2");
        f.RoadScore.Should().Be("1");
        f.Result.Should().BeTrue();
    }
}