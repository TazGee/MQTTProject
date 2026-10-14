using MQTTGitProject.Config;

namespace MQTTDevice.Domain.Services;

public interface IMqttService
{
    float VrednostSimulatora { get; set; }
    string DeviceValueTopic { get; set; }
    Task PublishMessageAsync(string topic, string message);
    Task InitializeService(Config config);
    Task SubscribeAsync();
}