# LeagueRepublic ICS Generator

Generate iCalendar (.ics) files for LeagueRepublic divisions from the public LeagueRepublic API.

## Disclaimer

This project is unofficial and not affiliated, associated, authorized, endorsed by, or in any way officially connected with League Republic, or any of its subsidiaries or affiliates.
All product and company names are trademarks™ or registered® trademarks of their respective holders.
Use of these names, trademarks, and brands does not imply endorsement.

This project makes use of the public League Republic API, but is an independent implementation. The maintainers of this project are not responsible for any changes or downtime of the official API.

# Introduction

This solution contains:
- LeagueRepublicApi — a small HTTP client for LeagueRepublic endpoints used by the tool
- LeagueRepublicConsole — a console app that outputs .ics files per division
- Unit tests for both projects

Example output files:
- Division 1.ics
- Division 2.ics

## Prerequisites
- .NET SDK 10.0 or later installed
- Internet access to reach https://api.leaguerepublic.com

## Quick Start

1) Clone the repository
- git clone https://github.com/<your-org-or-user>/LeagueRepublicIcs.git
- cd LeagueRepublicIcs

2) Restore and build
- dotnet build

3) Configure a default league id (optional)
The console app reads configuration from .NET User Secrets. You can set a default league id so you don’t have to pass it on every run.

- cd LeagueRepublicConsole
- dotnet user-secrets init
- dotnet user-secrets set "leagueid" "<your-league-id>"

4) Run
- Using default league id from user-secrets:
  - dotnet run -- ics
- Passing a league id explicitly (overrides user-secret):
  - dotnet run -- ics 123456

Output .ics files will be written to the working directory (repository root if run from solution root, or the console project directory if run there).

Notes:
- Two equivalent routes are available in the app:
  - ics
  - ics {leagueid}
- If no seasons are found for a league, the app will report an error.

## Project Details

### LeagueRepublicApi
- Provides typed methods for the following endpoints:
  - json/getSeasonsForLeague/{leagueId}.json
  - json/getFixtureGroupsForSeason/{seasonId}.json
  - json/getFixturesForSeason/{seasonId}.json
  - json/getFixturesForFixtureGroup/{fixtureGroupIdentifier}.json
- Default base URL: https://api.leaguerepublic.com (configurable through LeagueRepublicClientOptions)

### LeagueRepublicConsole
- Binds routes using TimeWarp.Nuru
- Command: ics [leagueid]
- Generates an .ics file per division for the current season (falls back to the first season if none marked current)
- Writes UTF-8 files without BOM

## Configuration

In order to call the API, the League Republic site must have the API enabled in the admin section. This is where the league id can be found. The league id appears to act as the API key for the League Republic API.

- leagueid (string):
  - When omitted from the command line, the app reads this from User Secrets.
  - Example user-secrets.json (managed by the CLI, do not edit manually):
    {
      "leagueid": "123456"
    }

## Running Tests
- dotnet test

## Troubleshooting
- Error: Missing 'leagueid' configuration value.
  - Set the league id in User Secrets or pass it as a command argument: dotnet run -- ics 123456
- No seasons returned for league.
  - Verify the league id is correct and that the API is reachable.

## Development Notes
- Target framework: net10.0
- Uses Microsoft.Extensions.Http for HttpClient and dependency injection
- Uses TimeWarp.Mediator and TimeWarp.Nuru for command routing and handling

## License

MIT License

Copyright (c) 2025 Stuart Grassie

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
