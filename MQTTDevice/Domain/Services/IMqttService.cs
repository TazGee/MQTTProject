using MQTTGitProject.Config;

namespace MQTTDevice.Domain.Services;

public interface IMqttService
{
    public float VrednostSimulatora { get; set; }
    Task PublishMessageAsync(string topic, string message);
    Task InitializeService(Config config);
    Task SubscribeAsync(string topic);
}