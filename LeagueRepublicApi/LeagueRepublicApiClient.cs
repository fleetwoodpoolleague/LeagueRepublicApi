using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using LeagueRepublicApi.Models.FixtureGroups;
using LeagueRepublicApi.Models.Seasons;
using LeagueRepublicApi.Models.Fixtures;

namespace LeagueRepublicApi;

/// <summary>
/// Concrete implementation of ILeagueRepublicApiClient using HttpClient.
/// </summary>
public sealed class LeagueRepublicApiClient : ILeagueRepublicApiClient
{
    private readonly HttpClient _httpClient;
    private readonly LeagueRepublicClientOptions _options;

    public LeagueRepublicApiClient(HttpClient httpClient, LeagueRepublicClientOptions? options = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? new LeagueRepublicClientOptions();
        if (_httpClient.BaseAddress is null)
        {
            _httpClient.BaseAddress = _options.BaseUri;
        }
    }

    public async Task<IReadOnlyList<Season>> GetSeasonsForLeagueAsync(long? leagueId = null, CancellationToken cancellationToken = default)
    {
        var id = leagueId ?? _options.LeagueId;
        if (id is null)
            throw new InvalidOperationException("A leagueId must be provided either in options or as a parameter.");

        var url = $"json/getSeasonsForLeague/{id}.json";
        var result = await _httpClient.GetFromJsonAsync<List<Season>>(url, JsonSerializerOptionsFactory.Options, cancellationToken).ConfigureAwait(false);
        return result ?? new List<Season>();
    }

    public async Task<IReadOnlyList<FixtureGroup>> GetFixtureGroupsForSeasonAsync(long seasonId, CancellationToken cancellationToken = default)
    {
        var url = $"json/getFixtureGroupsForSeason/{seasonId}.json";
        var result = await _httpClient.GetFromJsonAsync<List<FixtureGroup>>(url, JsonSerializerOptionsFactory.Options, cancellationToken).ConfigureAwait(false);
        return result ?? new List<FixtureGroup>();
    }

    public async Task<IReadOnlyList<Fixture>> GetFixturesForSeasonAsync(long seasonId, CancellationToken cancellationToken = default)
    {
        var url = $"json/getFixturesForSeason/{seasonId}.json";
        var result = await _httpClient.GetFromJsonAsync<List<Fixture>>(url, JsonSerializerOptionsFactory.Options, cancellationToken).ConfigureAwait(false);
        return result ?? new List<Fixture>();
    }

    public async Task<IReadOnlyList<Fixture>> GetFixturesForFixtureGroupAsync(long fixtureGroupIdentifier, CancellationToken cancellationToken = default)
    {
        var url = $"json/getFixturesForFixtureGroup/{fixtureGroupIdentifier}.json";
        var result = await _httpClient.GetFromJsonAsync<List<Fixture>>(url, JsonSerializerOptionsFactory.Options, cancellationToken).ConfigureAwait(false);
        return result ?? new List<Fixture>();
    }
}

internal static class JsonSerializerOptionsFactory
{
    private static System.Text.Json.JsonSerializerOptions? _options;
    public static System.Text.Json.JsonSerializerOptions Options => _options ??= Create();

    private static System.Text.Json.JsonSerializerOptions Create()
    {
        var opts = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        return opts;
    }
}