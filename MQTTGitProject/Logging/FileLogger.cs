namespace MQTTGitProject;

public class FileLogger : IMessageLogger
{
    private const string fileName = "mqtt_messages.log";
    
    public FileLogger()
    {
        if (!File.Exists(fileName)) File.Create(fileName);
    }
    
    public async void LogMessageAsync(string topic, string payload, string qos, string retain)
    {
        string line = $"\n[{DateTime.Now:HH:mm:sss}] " +
                      $"| {topic} | {payload} | {qos} | {retain}";
        await File.AppendAllTextAsync(fileName, line);
    }
}