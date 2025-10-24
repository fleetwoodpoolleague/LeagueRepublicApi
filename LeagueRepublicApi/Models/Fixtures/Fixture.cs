using System.Text.Json.Serialization;

namespace LeagueRepublicApi.Models.Fixtures;

public sealed class Fixture
{
    [JsonPropertyName("additionalScore")] public string? AdditionalScore { get; init; }

    [JsonPropertyName("fixtureDate")] public string? FixtureDateRaw { get; init; }

    [JsonPropertyName("fixtureDateInMilliseconds")] public long? FixtureDateInMilliseconds { get; init; }

    [JsonPropertyName("fixtureDateStatusDesc")] public string? FixtureDateStatusDesc { get; init; }

    [JsonPropertyName("fixtureDateStatusID")] public int? FixtureDateStatusId { get; init; }

    [JsonPropertyName("fixtureGroupDesc")] public string? FixtureGroupDesc { get; init; }

    [JsonPropertyName("fixtureGroupIdentifier")] public long? FixtureGroupIdentifier { get; init; }

    [JsonPropertyName("fixtureID")] public long FixtureId { get; init; }

    [JsonPropertyName("fixtureNote")] public string? FixtureNote { get; init; }

    [JsonPropertyName("fixtureStatus")] public int FixtureStatus { get; init; }

    [JsonPropertyName("fixtureStatusDesc")] public string? FixtureStatusDesc { get; init; }

    [JsonPropertyName("fixtureTypeID")] public int FixtureTypeId { get; init; }

    [JsonPropertyName("homeScore")] public string? HomeScore { get; init; }

    [JsonPropertyName("homeScoreNote")] public string? HomeScoreNote { get; init; }

    [JsonPropertyName("homeTeamName")] public string HomeTeamName { get; init; } = string.Empty;

    [JsonPropertyName("noResultOutcome")] public bool NoResultOutcome { get; init; }

    [JsonPropertyName("result")] public bool Result { get; init; }

    [JsonPropertyName("roadScore")] public string? RoadScore { get; init; }

    [JsonPropertyName("roadScoreNote")] public string? RoadScoreNote { get; init; }

    [JsonPropertyName("roadTeamName")] public string RoadTeamName { get; init; } = string.Empty;

    [JsonPropertyName("roundDesc")] public string? RoundDesc { get; init; }

    [JsonPropertyName("shortCode")] public string? ShortCode { get; init; }

    [JsonPropertyName("venueAndSubVenueDesc")] public string? VenueAndSubVenueDesc { get; init; }
}
