using System.Text;
using Microsoft.Extensions.Logging;

namespace LeagueRepublicConsole;

public sealed class PhysicalFileWriter(ILogger<PhysicalFileWriter> logger) : IFileWriter
{
    public void WriteAllText(string path, string contents)
    {
        logger.LogInformation("Writing file contents to {Path}", path);
        File.WriteAllText(path, contents, new UTF8Encoding(false));
    }

    public string ReadAllText(string path)
    {
        logger.LogInformation("Reading file contents from {Path}", path);
        return File.ReadAllText(path, new UTF8Encoding(false));
    }

    public IEnumerable<string> GetFiles(string directory, string searchPattern)
    {
        logger.LogInformation("Listing files in {Directory} matching {Pattern}", directory, searchPattern);
        return Directory.GetFiles(directory, searchPattern);
    }
}