namespace MQTTGitProject;

/// <summary>
/// Interfejs za definisanje loggera
/// </summary>
public interface IMessageLogger
{
    Task LogMessageAsync(string topic, string payload, string qos, string retain);
}