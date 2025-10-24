using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LeagueRepublicApi.Models.FixtureGroups;
using LeagueRepublicApi.Models.Seasons;

namespace LeagueRepublicApi;

/// <summary>
/// Abstraction for accessing the LeagueRepublic JSON API.
/// </summary>
public interface ILeagueRepublicApiClient
{
    /// <summary>
    /// Gets seasons for a league. The leagueId can be supplied here or via options at construction time.
    /// </summary>
    /// <param name="leagueId">Optional league identifier. If null, the value from options will be used.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<Season>> GetSeasonsForLeagueAsync(long? leagueId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets fixture groups for a given season.
    /// </summary>
    /// <param name="seasonId">The season identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<FixtureGroup>> GetFixtureGroupsForSeasonAsync(long seasonId, CancellationToken cancellationToken = default);
}