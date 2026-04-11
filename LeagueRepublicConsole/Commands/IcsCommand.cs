using LeagueRepublicApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TimeWarp.Nuru;

namespace LeagueRepublicConsole.Commands;

[NuruRoute("ics {leagueid?} --league-name {leaguename}", Description = "Generate an ics file for the given league.")]
public sealed class IcsCommand : ICommand<Unit>
{
    [Parameter(Description = "League ID to generate the ics file for. If not provided, will attempt to find the league ID from the league name.")]
    public string LeagueId { get; set; } = string.Empty;
    [Parameter(Description = "League name to generate the ics file for. Required if league ID is not provided.")]
    public string LeagueName { get; set; } = string.Empty;

    public sealed class Handler(ILogger<Handler> logger, FixturesIcsGenerator icsGenerator) : ICommandHandler<IcsCommand, Unit>
    {
        public async ValueTask<Unit> Handle(IcsCommand request, CancellationToken cancellationToken)
    {
            try
            {
                logger.LogInformation("Handling ics Command");
                await icsGenerator.RunAsync(request.LeagueId, request.LeagueName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected exception attempting to generate ics file(s).");
            }

            return default;
        }
    }
}