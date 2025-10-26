using System.Text;
using Microsoft.Extensions.Logging;

namespace LeagueRepublicConsole;

internal sealed class PhysicalFileWriter(ILogger<PhysicalFileWriter> logger) : IFileWriter
{
    public void WriteAllText(string path, string contents)
    {
        logger.LogInformation("Writing file contents to {Path}", path);
        File.WriteAllText(path, contents, new UTF8Encoding(false));
    }
}