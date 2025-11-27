using Microsoft.Extensions.Logging;
using TimeWarp.Mediator;

namespace LeagueRepublicConsole.Commands;

public sealed class TeamIcsCommand : IRequest
{
    public string LeagueId { get; set; } = string.Empty;
    public string LeagueName { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;

    public sealed class Handler(ILogger<Handler> logger, TeamFixturesIcsGenerator icsGenerator) : IRequestHandler<TeamIcsCommand>
    {
        public async Task Handle(TeamIcsCommand request, CancellationToken cancellationToken)
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
        }
    }
}