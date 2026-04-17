using Microsoft.Extensions.Logging;
using TimeWarp.Nuru;

namespace LeagueRepublicConsole.Commands;

[NuruRoute("update", Description = "Update completion status of competition JSON files in the given directory.")]
public sealed class CompetitionsUpdateCommand : ICommand<Unit>
{
    [Parameter(Description = "Path to the directory containing competition JSON files.")]
    public string Directory { get; set; } = string.Empty;

    public sealed class Handler(
        ILogger<Handler> logger,
        CompetitionsCompletionUpdater updater) : ICommandHandler<CompetitionsUpdateCommand, Unit>
    {
        public async ValueTask<Unit> Handle(CompetitionsUpdateCommand request, CancellationToken cancellationToken)
        {
            try
            {
                logger.LogInformation("Handling competitions update for directory: {Directory}", request.Directory);
                await updater.RunAsync(request.Directory);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected exception updating competition files.");
            }
            return default;
        }
    }
}
