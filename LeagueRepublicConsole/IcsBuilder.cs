using System.Globalization;
using System.Text;
using LeagueRepublicApi.Models.Fixtures;

namespace LeagueRepublicConsole;

/// <summary>
/// Builds RFC 5545 iCalendar content for a set of fixtures.
/// Shared by the per-division and per-team generators.
/// </summary>
public static class IcsBuilder
{
    public static readonly TimeSpan DefaultEventDuration = TimeSpan.FromHours(2.5);

    public static string Build(string? leagueName, string calendarName, IEnumerable<Fixture> fixtures, DateTime dtStampUtc)
    {
        var trimmedCalendarName = calendarName.Trim();
        var calendarTitle = string.IsNullOrWhiteSpace(leagueName)
            ? Escape(trimmedCalendarName)
            : $"{Escape(leagueName.Trim())}: {Escape(trimmedCalendarName)}";
        var sb = new StringBuilder();
        sb.Append("BEGIN:VCALENDAR\r\n");
        sb.Append("VERSION:2.0\r\n");
        sb.Append("PRODID:-//github.com/sgrassie/LeagueRepublicConsole//EN\r\n");
        sb.Append("X-WR-CALNAME:").Append(calendarTitle).Append("\r\n");
        sb.Append("X-WR-TIMEZONE:Europe/London\r\n");
        sb.Append("CALSCALE:GREGORIAN\r\n");

        var stamp = FormatDateTimeUtc(dtStampUtc);
        foreach (var f in fixtures.OrderBy(f => f.FixtureDateInMilliseconds ?? long.MaxValue))
        {
            sb.Append("BEGIN:VEVENT\r\n");
            sb.Append("UID:").Append(f.FixtureId).Append("@leaguerepublic\r\n");
            sb.Append("DTSTAMP:").Append(stamp).Append("\r\n");
            if (f.FixtureDateInMilliseconds is long ms)
            {
                var dt = DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime;
                sb.Append("DTSTART:").Append(FormatDateTimeUtc(dt)).Append("\r\n");
                sb.Append("DTEND:").Append(FormatDateTimeUtc(dt.Add(DefaultEventDuration))).Append("\r\n");
            }
            var summary = $"{f.HomeTeamName?.Trim()} vs {f.RoadTeamName?.Trim()}";
            sb.Append("SUMMARY:").Append(Escape(summary)).Append("\r\n");
            if (!string.IsNullOrWhiteSpace(f.VenueAndSubVenueDesc))
            {
                sb.Append("LOCATION:").Append(Escape(f.VenueAndSubVenueDesc!.Trim())).Append("\r\n");
            }
            var desc = BuildDescription(f);
            if (!string.IsNullOrEmpty(desc))
            {
                sb.Append("DESCRIPTION:").Append(Escape(desc)).Append("\r\n");
            }
            sb.Append("END:VEVENT\r\n");
        }
        sb.Append("END:VCALENDAR\r\n");
        return sb.ToString();
    }

    public static string BuildDescription(Fixture f)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(f.FixtureNote)) parts.Add(f.FixtureNote!);
        if (!string.IsNullOrWhiteSpace(f.FixtureStatusDesc)) parts.Add($"Status: {f.FixtureStatusDesc}");
        if (f.Result)
        {
            var score = $"Result: {f.HomeScore ?? ""}-{f.RoadScore ?? ""}".Trim();
            parts.Add(score);
        }
        return string.Join("; ", parts);
    }

    public static string FormatDateTimeUtc(DateTime dt)
        => dt.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);

    public static string Escape(string value)
    {
        // RFC 5545 escaping: backslash, semicolon, comma, and newlines
        return value
            .Replace("\\", "\\\\")
            .Replace(";", "\\;")
            .Replace(",", "\\,")
            .Replace("\r\n", "\\n")
            .Replace("\n", "\\n");
    }

    public static string MakeSafeFileName(string name)
    {
        var normalized = name.EndsWith(".ics", StringComparison.OrdinalIgnoreCase)
            ? name[..^4].Trim() + name[^4..]
            : name.Trim();

        foreach (var c in Path.GetInvalidFileNameChars())
            normalized = normalized.Replace(c, '_');

        return normalized.Replace(" ", "-");
    }
}
