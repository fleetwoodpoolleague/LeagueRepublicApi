using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using FluentAssertions;
using LeagueRepublicConsole;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LeagueRepublicConsole.Tests;

public class CompetitionsCompletionUpdaterTests
{
    private sealed class InMemoryFileWriter : IFileWriter
    {
        public Dictionary<string, string> Files { get; } = new();
        public void WriteAllText(string path, string contents) => Files[path] = contents;
        public string ReadAllText(string path) => Files[path];
        public IEnumerable<string> GetFiles(string directory, string searchPattern)
            => Files.Keys.Where(k => k.StartsWith(directory));
    }

    private static CompetitionsCompletionUpdater CreateUpdater(InMemoryFileWriter files)
        => new(NullLogger<CompetitionsCompletionUpdater>.Instance, files);

    // ── UpdateCompletionStatus (pure logic, no file I/O) ─────────────────────

    [Fact]
    public void UpdateCompletionStatus_Returns_False_When_No_Dates_Array()
    {
        var root = JsonNode.Parse("""{"completed": false}""")!;
        var changed = CompetitionsCompletionUpdater.UpdateCompletionStatus(root, new DateOnly(2026, 4, 17));
        changed.Should().BeFalse();
    }

    [Fact]
    public void UpdateCompletionStatus_Returns_False_When_No_Past_Incomplete_Dates()
    {
        var json = """
        {
            "completed": false,
            "dates": [
                { "name": "Week 1", "date": "2026-04-01", "completed": true },
                { "name": "Week 2", "date": "2026-04-30", "completed": false }
            ]
        }
        """;
        var root = JsonNode.Parse(json)!;
        var changed = CompetitionsCompletionUpdater.UpdateCompletionStatus(root, new DateOnly(2026, 4, 17));
        changed.Should().BeFalse();
    }

    [Fact]
    public void UpdateCompletionStatus_Marks_Past_Incomplete_Entry_Completed()
    {
        var json = """
        {
            "completed": false,
            "dates": [
                { "name": "Week 1", "date": "2026-04-15", "completed": false }
            ]
        }
        """;
        var root = JsonNode.Parse(json)!;
        CompetitionsCompletionUpdater.UpdateCompletionStatus(root, new DateOnly(2026, 4, 17));
        root["dates"]![0]!["completed"]!.GetValue<bool>().Should().BeTrue();
    }

    [Fact]
    public void UpdateCompletionStatus_Skips_TBC_Dates()
    {
        var json = """
        {
            "completed": false,
            "dates": [
                { "name": "Grand Final", "date": "TBC", "completed": false }
            ]
        }
        """;
        var root = JsonNode.Parse(json)!;
        var changed = CompetitionsCompletionUpdater.UpdateCompletionStatus(root, new DateOnly(2026, 4, 17));
        changed.Should().BeFalse();
        root["dates"]![0]!["completed"]!.GetValue<bool>().Should().BeFalse();
    }

    [Fact]
    public void UpdateCompletionStatus_Does_Not_Change_Already_Completed_Entry()
    {
        var json = """
        {
            "completed": false,
            "dates": [
                { "name": "Week 1", "date": "2026-04-01", "completed": true }
            ]
        }
        """;
        var root = JsonNode.Parse(json)!;
        var changed = CompetitionsCompletionUpdater.UpdateCompletionStatus(root, new DateOnly(2026, 4, 17));
        changed.Should().BeFalse();
    }

    [Fact]
    public void UpdateCompletionStatus_Sets_Root_Completed_When_All_Dates_Completed()
    {
        var json = """
        {
            "completed": false,
            "dates": [
                { "name": "Week 1", "date": "2026-04-01", "completed": true },
                { "name": "Week 2", "date": "2026-04-15", "completed": false }
            ]
        }
        """;
        var root = JsonNode.Parse(json)!;
        CompetitionsCompletionUpdater.UpdateCompletionStatus(root, new DateOnly(2026, 4, 17));
        root["completed"]!.GetValue<bool>().Should().BeTrue();
    }

    [Fact]
    public void UpdateCompletionStatus_Does_Not_Set_Root_Completed_When_Future_Dates_Remain()
    {
        var json = """
        {
            "completed": false,
            "dates": [
                { "name": "Week 1", "date": "2026-04-15", "completed": false },
                { "name": "Grand Final", "date": "TBC", "completed": false }
            ]
        }
        """;
        var root = JsonNode.Parse(json)!;
        CompetitionsCompletionUpdater.UpdateCompletionStatus(root, new DateOnly(2026, 4, 17));
        root["completed"]!.GetValue<bool>().Should().BeFalse();
    }

    [Fact]
    public void UpdateCompletionStatus_Returns_True_When_An_Entry_Changed()
    {
        var json = """
        {
            "completed": false,
            "dates": [
                { "name": "Week 1", "date": "2026-04-15", "completed": false }
            ]
        }
        """;
        var root = JsonNode.Parse(json)!;
        var changed = CompetitionsCompletionUpdater.UpdateCompletionStatus(root, new DateOnly(2026, 4, 17));
        changed.Should().BeTrue();
    }

    // ── ProcessFile (idempotency and write gate) ──────────────────────────────

    [Fact]
    public void ProcessFile_Does_Not_Write_When_No_Changes()
    {
        var files = new InMemoryFileWriter();
        var original = """
        {
            "completed": false,
            "dates": [
                { "name": "Week 1", "date": "2026-04-30", "completed": false }
            ]
        }
        """;
        files.Files["dir/comp.json"] = original;
        var updater = CreateUpdater(files);

        // Pass a fixed today (2026-04-17) that is before the entry date (2026-04-30)
        updater.ProcessFile("dir/comp.json", new DateOnly(2026, 4, 17));

        files.Files["dir/comp.json"].Should().Be(original);
    }

    [Fact]
    public void ProcessFile_Writes_Updated_Json_When_Past_Entry_Found()
    {
        var files = new InMemoryFileWriter();
        files.Files["dir/comp.json"] = """
        {
            "completed": false,
            "dates": [
                { "name": "Week 1", "date": "2020-01-01", "completed": false }
            ]
        }
        """;
        var updater = CreateUpdater(files);
        updater.ProcessFile("dir/comp.json", new DateOnly(2026, 4, 17));

        var updated = JsonNode.Parse(files.Files["dir/comp.json"])!;
        updated["dates"]![0]!["completed"]!.GetValue<bool>().Should().BeTrue();
    }

    // ── RunAsync (end-to-end with in-memory file system) ─────────────────────

    [Fact]
    public async Task RunAsync_Processes_All_Json_Files_In_Directory()
    {
        var files = new InMemoryFileWriter();
        files.Files["mydir/a.json"] = """
        {"completed": false, "dates": [{"name": "W1", "date": "2020-01-01", "completed": false}]}
        """;
        files.Files["mydir/b.json"] = """
        {"completed": false, "dates": [{"name": "W1", "date": "2020-01-01", "completed": false}]}
        """;
        var updater = CreateUpdater(files);
        await updater.RunAsync("mydir", new DateOnly(2026, 4, 17));

        JsonNode.Parse(files.Files["mydir/a.json"])!["dates"]![0]!["completed"]!
            .GetValue<bool>().Should().BeTrue();
        JsonNode.Parse(files.Files["mydir/b.json"])!["dates"]![0]!["completed"]!
            .GetValue<bool>().Should().BeTrue();
    }

    [Fact]
    public async Task RunAsync_Throws_ArgumentException_For_Empty_Directory()
    {
        var files = new InMemoryFileWriter();
        var updater = CreateUpdater(files);
        var act = async () => await updater.RunAsync(string.Empty);
        await act.Should().ThrowAsync<ArgumentException>();
    }
}
