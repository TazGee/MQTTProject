namespace MQTTGitProject;

/// <summary>
/// Logger koji ispisuje poruke u fajlu
/// </summary>
public class FileLogger : IMessageLogger
{
    private string fileName;
    
    public FileLogger(Config.Config config)
    {
        fileName = config.LogFilePath;
    }
    /// <summary>
    /// Ispisuje poruku u fajlu
    /// </summary>
    /// <param name="topic">Topic poruke</param>
    /// <param name="payload">Sadrzaj poruke</param>
    /// <param name="qos">Quality of Service poruke</param>
    /// <param name="retain">Retain poruke</param>
    public async Task LogMessageAsync(string topic, string payload, string qos, string retain)
    {
        try
        {
            string line = $"\n[{DateTime.Now:HH:mm:ss}] " +
                          $"| {topic} | {payload} | {qos} | {retain}";
            await File.AppendAllTextAsync(fileName, line);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Greska prilikom logovanja:  {e.Message}");
        }
    }
}