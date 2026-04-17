using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

namespace LeagueRepublicConsole;

public sealed class CompetitionsCompletionUpdater(
    ILogger<CompetitionsCompletionUpdater> logger,
    IFileWriter files)
{
    private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

    public Task RunAsync(string directory, DateOnly? today = null)
    {
        if (string.IsNullOrWhiteSpace(directory))
            throw new ArgumentException("Directory must be provided.", nameof(directory));

        foreach (var filePath in files.GetFiles(directory, "*.json"))
        {
            logger.LogDebug("Processing {FilePath}", filePath);
            ProcessFile(filePath, today);
        }

        return Task.CompletedTask;
    }

    internal void ProcessFile(string filePath, DateOnly? today = null)
    {
        var raw = files.ReadAllText(filePath);
        var root = JsonNode.Parse(raw);
        if (root is null)
        {
            logger.LogWarning("Could not parse JSON in {FilePath}, skipping.", filePath);
            return;
        }

        var effectiveToday = today ?? DateOnly.FromDateTime(DateTime.Today);
        var changed = UpdateCompletionStatus(root, effectiveToday);

        if (!changed)
        {
            logger.LogDebug("No changes to {FilePath}, skipping write.", filePath);
            return;
        }

        files.WriteAllText(filePath, root.ToJsonString(WriteOptions));
        logger.LogInformation("Updated {FilePath}", filePath);
    }

    public static bool UpdateCompletionStatus(JsonNode root, DateOnly today)
    {
        var datesArray = root["dates"]?.AsArray();
        if (datesArray is null) return false;

        var changed = false;

        foreach (var entry in datesArray)
        {
            if (entry is null) continue;
            if (entry["completed"]?.GetValue<bool>() == true) continue;

            var dateStr = entry["date"]?.GetValue<string>();
            if (dateStr is null) continue;
            if (!DateOnly.TryParse(dateStr, out var entryDate)) continue; // "TBC" etc.

            if (entryDate < today)
            {
                entry["completed"] = true;
                changed = true;
            }
        }

        if (!changed) return false;

        // Promote root completed when every entry is done
        var allCompleted = datesArray.All(e => e?["completed"]?.GetValue<bool>() == true);
        if (allCompleted && root["completed"]?.GetValue<bool>() != true)
            root["completed"] = true;

        return true;
    }
}
