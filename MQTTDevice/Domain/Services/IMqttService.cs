using MQTTDevice.Config;

namespace MQTTDevice.Domain.Services;

public interface IMqttService
{
    float VrednostSimulatora { get; set; }
    Task PublishMessageAsync(string topic, string message);
    Task InitializeService(Config.Config config);
    Task SubscribeAsync(string topic);
}