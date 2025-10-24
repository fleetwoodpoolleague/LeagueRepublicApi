using System;

namespace LeagueRepublicApi;

public sealed class LeagueRepublicClientOptions
{
    /// <summary>
    /// Base URL of the LeagueRepublic API. Default: https://api.leaguerepublic.com
    /// </summary>
    public Uri BaseUri { get; init; } = new("https://api.leaguerepublic.com");

    /// <summary>
    /// Optional default league id that can be used when not supplied per call.
    /// </summary>
    public long? LeagueId { get; init; }
}
