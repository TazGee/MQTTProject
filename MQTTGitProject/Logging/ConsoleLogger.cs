namespace MQTTGitProject;

public class ConsoleLogger : IMessageLogger
{
    
    public async void LogMessageAsync(string topic, string payload, string qos, string retain)
    {
        string line = $"\n[{DateTime.Now:HH:mm:sss}] " +
                      $"| {topic} | {payload} | {qos} | {retain}";
        Console.WriteLine(line);
    }
}