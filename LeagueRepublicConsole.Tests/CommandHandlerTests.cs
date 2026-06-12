using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using LeagueRepublicApi;
using LeagueRepublicApi.Models.FixtureGroups;
using LeagueRepublicApi.Models.Fixtures;
using LeagueRepublicApi.Models.Seasons;
using LeagueRepublicConsole.Commands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LeagueRepublicConsole.Tests;

public class CommandHandlerTests
{
    private sealed class ThrowingApiClient : ILeagueRepublicApiClient
    {
        public Task<IReadOnlyList<Season>> GetSeasonsForLeagueAsync(long? leagueId = null, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("API unavailable");
        public Task<IReadOnlyList<FixtureGroup>> GetFixtureGroupsForSeasonAsync(long seasonId, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("API unavailable");
        public Task<IReadOnlyList<Fixture>> GetFixturesForSeasonAsync(long seasonId, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("API unavailable");
        public Task<IReadOnlyList<Fixture>> GetFixturesForFixtureGroupAsync(long fixtureGroupIdentifier, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("API unavailable");
    }

    private sealed class InMemoryFileWriter : IFileWriter
    {
        public Dictionary<string, string> Files { get; } = new();
        public void WriteAllText(string path, string contents) => Files[path] = contents;
        public string ReadAllText(string path) => Files[path];
        public IEnumerable<string> GetFiles(string directory, string searchPattern)
            => Files.Keys.Where(k => k.StartsWith(directory));
    }

    private static IConfiguration EmptyConfig() => new ConfigurationBuilder().Build();

    [Fact]
    public async Task IcsCommand_Handler_Propagates_Generator_Failures()
    {
        var generator = new FixturesIcsGenerator(
            NullLogger<FixturesIcsGenerator>.Instance, EmptyConfig(), new ThrowingApiClient(), new InMemoryFileWriter());
        var handler = new IcsCommand.Handler(NullLogger<IcsCommand.Handler>.Instance, generator);

        var act = async () => await handler.Handle(new IcsCommand { LeagueId = "123" }, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task TeamIcsCommand_Handler_Propagates_Generator_Failures()
    {
        var generator = new TeamFixturesIcsGenerator(
            NullLogger<TeamFixturesIcsGenerator>.Instance, EmptyConfig(), new ThrowingApiClient(), new InMemoryFileWriter());
        var handler = new TeamIcsCommand.Handler(NullLogger<TeamIcsCommand.Handler>.Instance, generator);

        var act = async () => await handler.Handle(
            new TeamIcsCommand { LeagueId = "123", TeamName = "Alpha" }, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CompetitionsUpdateCommand_Handler_Propagates_Updater_Failures()
    {
        var updater = new CompetitionsCompletionUpdater(
            NullLogger<CompetitionsCompletionUpdater>.Instance, new InMemoryFileWriter());
        var handler = new CompetitionsUpdateCommand.Handler(NullLogger<CompetitionsUpdateCommand.Handler>.Instance, updater);

        var act = async () => await handler.Handle(
            new CompetitionsUpdateCommand { Directory = string.Empty }, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }
}
