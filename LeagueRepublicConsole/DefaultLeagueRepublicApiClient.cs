using LeagueRepublicApi;

namespace LeagueRepublicConsole;

public sealed class DefaultLeagueRepublicApiClient(LeagueRepublicClientOptions options)
    : LeagueRepublicApiClient(new HttpClient(), options);
