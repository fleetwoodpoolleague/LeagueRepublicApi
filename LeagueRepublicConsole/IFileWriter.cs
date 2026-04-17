namespace LeagueRepublicConsole;

public interface IFileWriter
{
    void WriteAllText(string path, string contents);
    string ReadAllText(string path);
    IEnumerable<string> GetFiles(string directory, string searchPattern);
}