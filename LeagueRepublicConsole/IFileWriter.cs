namespace LeagueRepublicConsole;

public interface IFileWriter
{
    void WriteAllText(string path, string contents);
}