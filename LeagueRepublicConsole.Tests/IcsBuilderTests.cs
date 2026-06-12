using System;
using System.Collections.Generic;
using FluentAssertions;
using LeagueRepublicApi.Models.Fixtures;
using Xunit;

namespace LeagueRepublicConsole.Tests;

public class IcsBuilderTests
{
    private static readonly DateTime Stamp = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

    private static Fixture MakeFixture(long id = 1, string home = "Home", string road = "Road", long? dateMs = 1730000000000)
        => new() { FixtureId = id, HomeTeamName = home, RoadTeamName = road, FixtureDateInMilliseconds = dateMs };

    [Fact]
    public void Build_Produces_Calendar_Skeleton_With_Title()
    {
        var ics = IcsBuilder.Build("My League", "Division A", new List<Fixture>(), Stamp);

        ics.Should().StartWith("BEGIN:VCALENDAR\r\nVERSION:2.0\r\nPRODID:-//github.com/sgrassie/LeagueRepublicConsole//EN\r\n");
        ics.Should().Contain("X-WR-CALNAME:My League: Division A\r\n");
        ics.Should().EndWith("END:VCALENDAR\r\n");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Build_Omits_League_Prefix_When_League_Name_Missing(string? leagueName)
    {
        var ics = IcsBuilder.Build(leagueName, "Division A", new List<Fixture>(), Stamp);
        ics.Should().Contain("X-WR-CALNAME:Division A\r\n");
    }

    [Fact]
    public void Escape_Handles_Rfc5545_Special_Characters()
    {
        IcsBuilder.Escape(@"a\b").Should().Be(@"a\\b");
        IcsBuilder.Escape("a;b").Should().Be(@"a\;b");
        IcsBuilder.Escape("a,b").Should().Be(@"a\,b");
        IcsBuilder.Escape("a\r\nb").Should().Be(@"a\nb");
        IcsBuilder.Escape("a\nb").Should().Be(@"a\nb");
    }

    [Fact]
    public void Escape_Does_Not_Escape_Forward_Slash()
    {
        // "\/" is not a valid RFC 5545 escape sequence
        IcsBuilder.Escape("AC/DC").Should().Be("AC/DC");
    }

    [Fact]
    public void Build_Escapes_Summary_And_Location()
    {
        var fixture = new Fixture
        {
            FixtureId = 1,
            HomeTeamName = "Dog; Duck",
            RoadTeamName = "Crown, Anchor",
            FixtureDateInMilliseconds = 1730000000000,
            VenueAndSubVenueDesc = "Back Room, Crown"
        };

        var ics = IcsBuilder.Build("L", "C", new List<Fixture> { fixture }, Stamp);

        ics.Should().Contain(@"SUMMARY:Dog\; Duck vs Crown\, Anchor");
        ics.Should().Contain(@"LOCATION:Back Room\, Crown");
    }

    [Fact]
    public void Build_Orders_Events_By_Date_With_Undated_Last()
    {
        var fixtures = new List<Fixture>
        {
            MakeFixture(id: 1, dateMs: null),
            MakeFixture(id: 2, dateMs: 1730003600000),
            MakeFixture(id: 3, dateMs: 1730000000000)
        };

        var ics = IcsBuilder.Build("L", "C", fixtures, Stamp);

        var posThird = ics.IndexOf("UID:3@leaguerepublic", StringComparison.Ordinal);
        var posSecond = ics.IndexOf("UID:2@leaguerepublic", StringComparison.Ordinal);
        var posFirst = ics.IndexOf("UID:1@leaguerepublic", StringComparison.Ordinal);
        posThird.Should().BeLessThan(posSecond);
        posSecond.Should().BeLessThan(posFirst);
    }

    [Fact]
    public void Build_Omits_Dtstart_For_Undated_Fixture()
    {
        var ics = IcsBuilder.Build("L", "C", new List<Fixture> { MakeFixture(dateMs: null) }, Stamp);
        ics.Should().NotContain("DTSTART");
    }

    [Fact]
    public void Build_Uses_Single_Dtstamp_For_All_Events()
    {
        var fixtures = new List<Fixture> { MakeFixture(id: 1), MakeFixture(id: 2) };
        var ics = IcsBuilder.Build("L", "C", fixtures, Stamp);

        var occurrences = ics.Split("DTSTAMP:20260601T120000Z\r\n").Length - 1;
        occurrences.Should().Be(2);
    }

    [Fact]
    public void BuildDescription_Combines_Note_Status_And_Result()
    {
        var fixture = new Fixture
        {
            FixtureId = 1,
            HomeTeamName = "Home",
            RoadTeamName = "Road",
            FixtureNote = "Rearranged",
            FixtureStatusDesc = "Played",
            Result = true,
            HomeScore = "5",
            RoadScore = "3"
        };

        IcsBuilder.BuildDescription(fixture).Should().Be("Rearranged; Status: Played; Result: 5-3");
    }

    [Fact]
    public void BuildDescription_Returns_Empty_When_Nothing_To_Say()
    {
        IcsBuilder.BuildDescription(MakeFixture()).Should().BeEmpty();
    }

    [Fact]
    public void BuildDescription_Handles_Result_With_Missing_Scores()
    {
        var fixture = new Fixture
        {
            FixtureId = 1,
            HomeTeamName = "Home",
            RoadTeamName = "Road",
            Result = true
        };

        IcsBuilder.BuildDescription(fixture).Should().Be("Result: -");
    }

    [Fact]
    public void MakeSafeFileName_Replaces_Invalid_Chars_And_Spaces()
    {
        IcsBuilder.MakeSafeFileName("AC/DC Stars.ics").Should().Be("AC_DC-Stars.ics");
    }
}
