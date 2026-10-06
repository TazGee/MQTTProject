namespace MQTTGitProject;

public class ConsoleLogger : IMessageLogger
{
    public Task LogMessageAsync(string topic, string payload, string qos, string retain)
    {
        try
        {
            string line = $"\n[{DateTime.Now:HH:mm:ss}] " +
                          $"| {topic} | {payload} | {qos} | {retain}";
            Console.WriteLine(line);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Greska prilikom logovanja:  {e.Message}");
        }
        return Task.CompletedTask;
    }
}