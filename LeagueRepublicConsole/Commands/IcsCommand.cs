using LeagueRepublicApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TimeWarp.Mediator;

namespace LeagueRepublicConsole.Commands;

public sealed class IcsCommand : IRequest
{
    public string LeagueId { get; set; } = string.Empty;

    public sealed class Handler(ILogger<Handler> logger, FixturesIcsGenerator icsGenerator) : IRequestHandler<IcsCommand>
    {
        public async Task Handle(IcsCommand request, CancellationToken cancellationToken)
        {
            try
            {
                logger.LogInformation("Handling ics Command");
                await icsGenerator.RunAsync(request.LeagueId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected exception attempting to generate ics file(s).");
            }
        }
    }
}