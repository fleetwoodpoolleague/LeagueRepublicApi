using LeagueRepublicApi;
using Microsoft.Extensions.Configuration;
using TimeWarp.Mediator;

namespace LeagueRepublicConsole.Commands;

public sealed class IcsCommand : IRequest
{
    public string LeagueId { get; set; } = string.Empty;

    public sealed class Handler(FixturesIcsGenerator icsGenerator) : IRequestHandler<IcsCommand>
    {
        public async Task Handle(IcsCommand request, CancellationToken cancellationToken)
        {
            try
            {
                await icsGenerator.RunAsync(request.LeagueId);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"LeagueRepublicConsole: {ex.Message}");
                Console.Error.WriteLine(ex.ToString());
            }
        }
    }
}