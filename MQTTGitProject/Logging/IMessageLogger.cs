namespace MQTTGitProject;

public interface IMessageLogger
{
    Task LogMessageAsync(string topic, string payload, string qos, string retain);
}