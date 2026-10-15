using MQTTDevice.Config;

namespace MQTTDevice.Domain.Services;

public interface IMqttService
{
    float VrednostSimulatora { get; set; }
    string DeviceValueTopic { get; set; }
    Task PublishMessageAsync(string topic);
    Task InitializeService(Config.Config config);
    Task SubscribeAsync();
}