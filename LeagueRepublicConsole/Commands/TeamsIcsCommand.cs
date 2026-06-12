using Microsoft.Extensions.Logging;
using TimeWarp.Nuru;

namespace LeagueRepublicConsole.Commands;

[NuruRouteGroup("ics")]
[NuruRoute("teams", Description = "Generate an ics file for every team in the given league.")]
public sealed class TeamsIcsCommand : ICommand<Unit>
{
    [Parameter(Description = "The league ID to generate per-team ics files for.")]
    public string LeagueId { get; set; } = string.Empty;
    [Option("--league-name", "", Description = "The league name used in each calendar's title.")]
    public string LeagueName { get; set; } = string.Empty;

    public sealed class Handler(ILogger<Handler> logger, TeamFixturesIcsGenerator icsGenerator) : ICommandHandler<TeamsIcsCommand, Unit>
    {
        public async ValueTask<Unit> Handle(TeamsIcsCommand request, CancellationToken cancellationToken)
        {
            try
            {
                logger.LogInformation("Handling teams ics Command");
                await icsGenerator.RunAllAsync(request.LeagueId, request.LeagueName, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected exception attempting to generate ics file(s).");
                throw;
            }

            return default;
        }
    }
}
