namespace MQTTGitProject;

/// <summary>
/// Logger koji ispisuje poruke u konzoli
/// </summary>
public class ConsoleLogger : IMessageLogger
{
    /// <summary>
    /// Ispisuje poruku u konzoli
    /// </summary>
    /// <param name="topic">Topic poruke</param>
    /// <param name="payload">Sadrzaj poruke</param>
    /// <param name="qos">Quality of Service poruke</param>
    /// <param name="retain">Retain poruke</param>
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