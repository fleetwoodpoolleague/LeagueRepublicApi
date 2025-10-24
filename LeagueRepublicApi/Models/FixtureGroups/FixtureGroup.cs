using System.Text.Json.Serialization;

namespace LeagueRepublicApi.Models.FixtureGroups;

public sealed class FixtureGroup
{
    [JsonPropertyName("fixtureGroupDesc")] public string FixtureGroupDesc { get; init; } = string.Empty;

    [JsonPropertyName("fixtureGroupIdentifier")] public long FixtureGroupIdentifier { get; init; }

    [JsonPropertyName("fixtureTypeDesc")] public string FixtureTypeDesc { get; init; } = string.Empty;

    [JsonPropertyName("fixtureTypeID")] public int FixtureTypeId { get; init; }
}
