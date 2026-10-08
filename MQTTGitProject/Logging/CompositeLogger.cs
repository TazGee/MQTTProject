namespace MQTTGitProject;

/// <summary>
/// Logger koji ispisuje poruku i u konzoli i u fajlu
/// </summary>
public class CompositeLogger : IMessageLogger
{
    private FileLogger fileLogger;
    private ConsoleLogger consoleLogger;
    
    public CompositeLogger(FileLogger fileLogger, ConsoleLogger consoleLogger)
    {
        this.fileLogger = fileLogger;
        this.consoleLogger = consoleLogger;
    }
    /// <summary>
    /// Ispisuje poruku u konzoli i fajlu
    /// </summary>
    /// <param name="topic">Topic poruke</param>
    /// <param name="payload">Sadrzaj poruke</param>
    /// <param name="qos">Quality of Service poruke</param>
    /// <param name="retain">Retain poruke</param>
    public async Task LogMessageAsync(string topic, string payload, string qos, string retain)
    {
        string line = $"\n[{DateTime.Now:HH:mm:ss}] " +
                      $"| {topic} | {payload} | {qos} | {retain}";
        
        await fileLogger.LogMessageAsync(topic, payload, qos, retain);
        await consoleLogger.LogMessageAsync(topic, payload, qos, retain);
    }
}