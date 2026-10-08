namespace MQTTDevice.Domain.Services;

public interface IMqttService
{
    Task PublishMessageAsync();
}