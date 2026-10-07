namespace MQTTGitProject;

public class FileLogger : IMessageLogger
{
    private string fileName;
    
    public FileLogger(Config.Config config)
    {
        fileName = config.LogFilePath;
    }
    
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