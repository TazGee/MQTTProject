namespace MQTTGitProject;

public class MessageLogger
{
    private const string fileName = "mqtt_messages.log";
    LogType logType = LogType.Console;
    
    public MessageLogger(LogType type)
    {
        logType = type;
        if (!File.Exists(fileName)) File.Create(fileName);
    }
    
    public async Task LogMessageAsync(string topic, string payload, string qos, string retain)
    {
        string line = $"\n[{DateTime.Now.Hour}:{DateTime.Now.Minute}:{DateTime.Now.Second}] | {topic} | {payload} | {qos} | {retain}";
        if(logType == LogType.File) await File.AppendAllTextAsync(fileName, line);
        else Console.WriteLine(line);
    }
}

public enum LogType
{
    Console,
    File
}