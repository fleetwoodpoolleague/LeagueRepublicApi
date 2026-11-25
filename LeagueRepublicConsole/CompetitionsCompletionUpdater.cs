using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

namespace LeagueRepublicConsole;

public sealed class CompetitionsCompletionUpdater(
    ILogger<CompetitionsCompletionUpdater> logger,
    IFileWriter files)
{
    private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

    public Task RunAsync(string directory, DateOnly? today = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(directory))
            throw new ArgumentException("Directory must be provided.", nameof(directory));

        foreach (var filePath in files.GetFiles(directory, "*.json"))
        {
            cancellationToken.ThrowIfCancellationRequested();
            logger.LogDebug("Processing {FilePath}", filePath);
            ProcessFile(filePath, today);
        }

        return Task.CompletedTask;
    }

    internal void ProcessFile(string filePath, DateOnly? today = null)
    {
        var raw = files.ReadAllText(filePath);
        JsonNode? root;
        try
        {
            root = JsonNode.Parse(raw);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Could not parse JSON in {FilePath}, skipping.", filePath);
            return;
        }

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

            // Multi-day entries carry an endDate; completion waits for the final day
            var endDateStr = entry["endDate"]?.GetValue<string>();
            if (endDateStr is not null)
            {
                if (!DateOnly.TryParse(endDateStr, out var endDate)) continue;
                entryDate = endDate;
            }

            if (entryDate < today)
            {
                entry["completed"] = true;
                changed = true;
            }
        }

        // Promote root completed when every entry is done, even if no entry flipped on this run
        var allCompleted = datesArray.Count > 0 && datesArray.All(e => e?["completed"]?.GetValue<bool>() == true);
        if (allCompleted && root["completed"]?.GetValue<bool>() != true)
        {
            root["completed"] = true;
            changed = true;
        }

        return changed;
    }
}
