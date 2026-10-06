namespace MQTTGitProject;

public interface IMessageLogger
{
    public void LogMessageAsync(string topic, string payload, string qos, string retain);
}