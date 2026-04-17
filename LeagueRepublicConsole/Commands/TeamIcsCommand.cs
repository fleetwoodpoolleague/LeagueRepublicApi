using Microsoft.Extensions.Logging;
using TimeWarp.Nuru;

namespace LeagueRepublicConsole.Commands;

[NuruRouteGroup("ics")]
[NuruRoute("team", Description = "Generate an ics file for the given division and team.")]
public sealed class TeamIcsCommand : ICommand<Unit>
{
    [Parameter(Description = "The league ID of the division to generate ics files.")]
    public string LeagueId { get; set; } = string.Empty;
    [Option("--league-name", "", Description = "The league name of the division to generate ics files.")]
    public string LeagueName { get; set; } = string.Empty;
    [Option("--team-name", "", Description = "The team name of the division to generate ics files.")]
    public string TeamName { get; set; } = string.Empty;

    public sealed class Handler(ILogger<Handler> logger, TeamFixturesIcsGenerator icsGenerator) : ICommandHandler<TeamIcsCommand, Unit>
    {
        public async ValueTask<Unit> Handle(TeamIcsCommand request, CancellationToken cancellationToken)
        {
            try
            {
                logger.LogInformation("Handling ics Command");
                await icsGenerator.RunAsync(request.LeagueId, request.LeagueName, request.TeamName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected exception attempting to generate ics file(s).");
            }

            return default;
        }
    }
}