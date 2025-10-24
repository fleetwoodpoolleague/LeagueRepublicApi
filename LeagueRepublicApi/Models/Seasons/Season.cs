using System.Text.Json.Serialization;

namespace LeagueRepublicApi.Models.Seasons;

public sealed class Season
{
    [JsonPropertyName("currentSeason")] public bool CurrentSeason { get; init; }

    [JsonPropertyName("seasonEndDate")] public string? SeasonEndDateRaw { get; init; }

    [JsonPropertyName("seasonEndDateInMilliseconds")] public long? SeasonEndDateInMilliseconds { get; init; }

    [JsonPropertyName("seasonID")] public long SeasonId { get; init; }

    [JsonPropertyName("seasonName")] public string SeasonName { get; init; } = string.Empty;

    [JsonPropertyName("seasonStartDate")] public string? SeasonStartDateRaw { get; init; }

    [JsonPropertyName("seasonStartDateInMilliseconds")] public long? SeasonStartDateInMilliseconds { get; init; }
}
