namespace MQTTGitProject;

public class CompositeLogger : IMessageLogger
{
    private FileLogger fileLogger;
    private ConsoleLogger consoleLogger;
    
    public CompositeLogger(FileLogger fileLogger, ConsoleLogger consoleLogger)
    {
        this.fileLogger = fileLogger;
        this.consoleLogger = consoleLogger;
    }
    
    public async Task LogMessageAsync(string topic, string payload, string qos, string retain)
    {
        string line = $"\n[{DateTime.Now:HH:mm:ss}] " +
                      $"| {topic} | {payload} | {qos} | {retain}";
        
        await fileLogger.LogMessageAsync(topic, payload, qos, retain);
        await consoleLogger.LogMessageAsync(topic, payload, qos, retain);
    }
}