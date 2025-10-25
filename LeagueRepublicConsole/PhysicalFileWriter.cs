using System.Text;

namespace LeagueRepublicConsole;

internal sealed class PhysicalFileWriter : IFileWriter
{
    public void WriteAllText(string path, string contents) => File.WriteAllText(path, contents, new UTF8Encoding(false));
}