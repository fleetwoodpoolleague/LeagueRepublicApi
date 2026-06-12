[![CI Status](https://github.com/fleetwoodpoolleague/LeagueRepublicApi/actions/workflows/dotnet-desktop.yml/badge.svg)](https://github.com/fleetwoodpoolleague/LeagueRepublicApi/actions/workflows/dotnet-desktop.yml)

# LeagueRepublic ICS Generator

A .NET CLI tool that fetches fixture data from the [LeagueRepublic](https://www.leaguerepublic.com) API and generates RFC 5545-compliant `.ics` calendar files. Used by the Fleetwood Pool League to publish fixtures to calendar apps, with daily generation via GitHub Actions.

## Disclaimer

This project is unofficial and not affiliated with, associated with, authorized, endorsed by, or in any way officially connected with League Republic or any of its subsidiaries or affiliates. All product and company names are trademarks™ or registered® trademarks of their respective holders. Use of these names, trademarks, and brands does not imply endorsement.

This project uses the public League Republic API as an independent implementation. The maintainers are not responsible for any changes or downtime of the official API.

## Features

- Generate a calendar file per division for an entire league
- Generate a calendar file for a single team (home and away fixtures)
- Generate calendar files for every team in a league from a single fixtures fetch
- Automatically mark past tournament dates as completed in JSON data files, including multi-day competitions with an end date

## Solution Structure

| Project | Description |
|---|---|
| `LeagueRepublicApi` | Typed HTTP client library wrapping the LeagueRepublic JSON API |
| `LeagueRepublicConsole` | CLI executable — generates `.ics` files and updates tournament data |
| `LeagueRepublicApi.Tests` | Unit tests for the API client |
| `LeagueRepublicConsole.Tests` | Unit tests for the generators and updater |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Internet access to reach `https://api.leaguerepublic.com`

> The LeagueRepublic API must be enabled in the league's admin settings. The league ID shown there acts as the API key.

## Building and Testing

```bash
# Build
dotnet build

# Run all tests
dotnet test

# Run tests for a specific project
dotnet test LeagueRepublicConsole.Tests
dotnet test LeagueRepublicApi.Tests
```

## Commands

### `ics` — Generate per-division calendars

Generates one `.ics` file per division for the given league, written to the current directory.

```
ics <leagueId> [--league-name <name>]
```

| Argument | Required | Description |
|---|---|---|
| `leagueId` | Yes (or via user secrets) | LeagueRepublic league ID |
| `--league-name` | No | Sets the `X-WR-CALNAME` prefix in each calendar |

```bash
dotnet run --project LeagueRepublicConsole -- ics 123456
dotnet run --project LeagueRepublicConsole -- ics 123456 --league-name "Fleetwood Pool League"
```

Output files are written to the current directory, one per division (e.g. `Division-1.ics`, `Division-2.ics`).

### `team` — Generate a per-team calendar

Generates a single `.ics` file containing all fixtures (home and away) for the named team.

```
team <leagueId> [--league-name <name>] --team-name <name>
```

| Argument | Required | Description |
|---|---|---|
| `leagueId` | Yes (or via user secrets) | LeagueRepublic league ID |
| `--league-name` | No | Sets the `X-WR-CALNAME` in the calendar |
| `--team-name` | Yes | Team name to filter fixtures for (case-insensitive) |

```bash
dotnet run --project LeagueRepublicConsole -- team 123456 \
  --league-name "Fleetwood Pool League" \
  --team-name "Cons A"
```

### `teams` — Generate calendars for every team

Discovers every distinct team name from the current season's fixtures and writes one `.ics` file per team, using a single fixtures fetch.

```
teams <leagueId> [--league-name <name>]
```

| Argument | Required | Description |
|---|---|---|
| `leagueId` | Yes (or via user secrets) | LeagueRepublic league ID |
| `--league-name` | No | Sets the `X-WR-CALNAME` prefix in each calendar |

```bash
dotnet run --project LeagueRepublicConsole -- teams 123456 --league-name "Fleetwood Pool League"
```

### `update` — Mark completed tournament dates

Scans a directory of JSON tournament files and sets `"completed": true` on any dates that have passed. Entries with an `"endDate"` (multi-day competitions) are only completed once the end date has passed; entries with an unparseable date (e.g. `"TBC"`) are left alone, and files that fail to parse are skipped with a warning. If all dates in a file are complete, the root-level `"completed"` flag is also set.

```
update <directory>
```

| Argument | Required | Description |
|---|---|---|
| `directory` | Yes | Path to directory containing `*.json` tournament files |

```bash
dotnet run --project LeagueRepublicConsole -- update /path/to/tournaments
```

## Configuration

### Setting a default league ID (user secrets)

To avoid passing the league ID on every invocation, store it as a user secret:

```bash
cd LeagueRepublicConsole
dotnet user-secrets set "leagueid" "<your-league-id>"
```

User secrets are loaded when `DOTNET_ENVIRONMENT=Development` is set:

```bash
DOTNET_ENVIRONMENT=Development dotnet run --project LeagueRepublicConsole -- ics
```

## How ICS Generation Works

All generators follow the same flow:

1. Resolve the league ID from the CLI argument or user secrets
2. Fetch seasons from the API; select the one marked `CurrentSeason = true`, falling back to the first in the list
3. Fetch fixture groups and fixtures for the selected season (concurrently, for the `ics` command)
4. Filter — the `ics` command keeps only groups where `FixtureTypeId == 1` (league divisions); the `team` and `teams` commands filter fixtures by team name (case-insensitive, home or away)
5. Build RFC 5545 `VCALENDAR` content using `\r\n` line endings and correct property escaping (shared `IcsBuilder`)
6. Write UTF-8 (no BOM) `.ics` files via `IFileWriter`

Each fixture becomes a `VEVENT` with:

| ICS Property | Value |
|---|---|
| `UID` | `{fixtureId}@leaguerepublic` |
| `SUMMARY` | `{HomeTeam} vs {AwayTeam}` |
| `DTSTART` | UTC datetime converted from the API's Unix millisecond timestamp |
| `LOCATION` | Venue name (when present) |
| `DESCRIPTION` | Result, status, and any fixture notes |

## Architecture Notes

**Command routing** uses [TimeWarp.Nuru](https://github.com/TimeWarpEngineering/timewarp-nuru), a source-generator–based CLI framework. Commands are decorated with `[NuruRoute]` and discovered at compile time via `DiscoverEndpoints()`. The generator intercepts `app.RunAsync()` and emits static dispatch code — no runtime reflection.

**Testability** is provided by the `IFileWriter` and `ILeagueRepublicApiClient` interfaces. Tests use hand-rolled fakes (`InMemoryFileWriter`, `FakeApiClient`) rather than a mocking framework.

**HttpClient** is wrapped in a `DefaultLeagueRepublicApiClient` subclass with a constructor that Nuru's AOT service construction can resolve without `IHttpClientFactory`.

## CI/CD

GitHub Actions runs on every push and pull request to `main`, on manual dispatch, and on a daily schedule at 01:00 UTC. The workflow:

1. Builds and tests in Release configuration
2. Publishes the CLI binary
3. Runs `ics` for the configured league and `teams` for all of its teams
4. Pushes the generated `.ics` files to the [Fixtures repository](https://github.com/fleetwoodpoolleague/Fixtures)
5. Runs `update` against the tournament data and pushes any changes to the [fylde8ball repository](https://github.com/fleetwoodpoolleague/fylde8ball)

Commands exit with a non-zero status on failure, so a broken API or bad configuration fails the build rather than silently publishing nothing.

**Required secrets:**

| Secret | Purpose |
|---|---|
| `LEAGUE_ID` | LeagueRepublic league identifier |
| `PUSH_TOKEN` | GitHub token with write access to the Fixtures repo |
| `FYLDE_EIGHTBALL` | GitHub token with write access to the fylde8ball repo |

## Troubleshooting

**"Missing 'leagueid' configuration value"** — Set the league ID in user secrets or pass it explicitly: `dotnet run --project LeagueRepublicConsole -- ics 123456`

**No seasons returned** — Verify the league ID is correct and the API is enabled in the league's admin settings.

**User secrets not loaded** — Ensure `DOTNET_ENVIRONMENT=Development` is set in your environment.

## License

MIT License — Copyright (c) 2025 Stuart Grassie

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
