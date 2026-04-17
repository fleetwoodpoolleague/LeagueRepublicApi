# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Build
dotnet build

# Run all tests
dotnet test

# Run tests for a specific project
dotnet test LeagueRepublicConsole.Tests
dotnet test LeagueRepublicApi.Tests

# Run a single test
dotnet test --filter "FullyQualifiedName~TestMethodName"

# Run with explicit league ID (per-division ICS)
dotnet run --project LeagueRepublicConsole -- ics 123456

# Generate per-team ICS file
dotnet run --project LeagueRepublicConsole -- team 123456 --league-name "My League" --team-name "Team A"

# Update competition completion status
dotnet run --project LeagueRepublicConsole -- update /path/to/tournaments

# Set up user secrets for default league ID
cd LeagueRepublicConsole
dotnet user-secrets set "leagueid" "<your-league-id>"
# Note: user secrets require DOTNET_ENVIRONMENT=Development to be set
```

## Architecture

### Solution Structure

Four projects in `LeagueRepublicApi.sln`:
- `LeagueRepublicApi` — typed HTTP client library wrapping the public LeagueRepublic API
- `LeagueRepublicConsole` — CLI tool that generates `.ics` files using the API client
- `LeagueRepublicApi.Tests` — unit tests for the API client
- `LeagueRepublicConsole.Tests` — unit tests for the console generators

### Command Routing (TimeWarp.Nuru)

Commands are discovered automatically via `NuruApp.CreateBuilder().DiscoverEndpoints()`. Each command class is decorated with `[NuruRoute("...")]` (single-word verb) and optionally `[NuruRouteGroup("...")]` (for grouping/help only — the group prefix is NOT part of the matched route). Three commands: `IcsCommand` → `ics`, `TeamIcsCommand` → `team`, `CompetitionsUpdateCommand` → `update`.

### ICS Generation Flow

Both generators follow the same pattern:
1. Resolve league ID from CLI arg or `IConfiguration` (user secrets key `"leagueid"`)
2. Fetch seasons via API → select `CurrentSeason = true`, fall back to first
3. Fetch fixture groups and fixtures for the season
4. `FixturesIcsGenerator`: filters groups where `FixtureTypeId == 1` (divisions), writes one `.ics` per division
5. `TeamFixturesIcsGenerator`: filters fixtures by team name (home or away), writes one `.ics` for that team
6. ICS content is built manually with `StringBuilder` using `\r\n` line endings (RFC 5545)
7. Files are written via `IFileWriter` (abstraction over `File.WriteAllText`, UTF-8 no BOM)

### Testability Pattern

Tests use hand-rolled fakes (`FakeApiClient`, `InMemoryFileWriter`) rather than a mocking framework. `ILeagueRepublicApiClient` and `IFileWriter` are the two interfaces that enable this. Test projects use xUnit + FluentAssertions.

### API Client

`LeagueRepublicApiClient` uses `HttpClient` injected via `Microsoft.Extensions.Http`. Base URL defaults to `https://api.leaguerepublic.com`. JSON deserialization uses `PropertyNameCaseInsensitive = true` via a shared `JsonSerializerOptionsFactory`.
