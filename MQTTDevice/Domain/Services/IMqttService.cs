using MQTTGitProject.Config;

namespace MQTTDevice.Domain.Services;

public interface IMqttService
{
    Task PublishMessageAsync(string topic, string message);
    Task InitializeService(Config config);
}